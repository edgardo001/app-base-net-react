import { useRef, useEffect, useCallback } from 'react'

interface Particle {
  x: number
  y: number
  vx: number
  vy: number
  life: number
  color: string
  size: number
}

interface Firework {
  x: number
  y: number
  particles: Particle[]
}

interface Obstacle {
  worldX: number
  type: 'rock' | 'gap'
  width: number
  height: number
  passed: boolean
}

interface MotoJuegoProps {
  className?: string
}

const CW = 800
const CH = 400
const GRAVITY = 0.48
const JUMP_FORCE = -13
const ACCEL = 0.2
const BRAKE = 0.3
const MAX_SPEED = 5.5
const BIKE_SCREEN_X = 160
const WHEEL_R = 10
const BIKE_W = 44
const BIKE_H = 20
const FW_COLORS = ['#ff4444', '#44aaff', '#ffdd44', '#44ff66', '#cc44ff', '#ff8844']
const OBSTACLE_INTERVAL = 480

function terrainHeight(worldX: number): number {
  return CH - 70
    + Math.sin(worldX * 0.006) * 45
    + Math.sin(worldX * 0.02) * 22
    + Math.sin(worldX * 0.045) * 10
}

function terrainAngle(worldX: number): number {
  const h1 = terrainHeight(worldX - 1)
  const h2 = terrainHeight(worldX + 1)
  return Math.atan2(h2 - h1, 2)
}

function lerp(a: number, b: number, t: number): number {
  return a + (b - a) * t
}

function rand(min: number, max: number): number {
  return Math.random() * (max - min) + min
}

export function MotoJuego({ className }: MotoJuegoProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null)

  const gameRef = useRef({
    worldOffset: 0,
    bikeY: 0,
    bikeVY: 0,
    bikeVX: 2,
    rotation: 0,
    onGround: true,
    obstacles: [] as Obstacle[],
    nextObstacleWorldX: OBSTACLE_INTERVAL,
    score: 0,
    status: 'idle' as 'idle' | 'playing' | 'gameover' | 'victory',
    fireworks: [] as Firework[],
    fwTimer: 0,
    keys: new Set<string>(),
    spacePressed: false,
    animFrameId: 0,
  })

  const resetGame = useCallback(() => {
    const g = gameRef.current
    g.worldOffset = 0
    g.bikeY = terrainHeight(BIKE_SCREEN_X)
    g.bikeVY = 0
    g.bikeVX = 2
    g.rotation = 0
    g.onGround = true
    g.obstacles = []
    g.nextObstacleWorldX = OBSTACLE_INTERVAL
    g.score = 0
    g.status = 'playing'
    g.fireworks = []
    g.fwTimer = 0
    g.keys.clear()
    g.spacePressed = false
  }, [])

  useEffect(() => {
    const canvas = canvasRef.current
    if (!canvas) return
    const ctx = canvas.getContext('2d')
    if (!ctx) return

    const g = gameRef.current

    function spawnFirework() {
      const fx = rand(80, CW - 80)
      const fy = rand(40, CH - 120)
      const particles: Particle[] = []
      const color = FW_COLORS[Math.floor(Math.random() * FW_COLORS.length)]
      const count = Math.floor(rand(20, 35))
      for (let i = 0; i < count; i++) {
        const angle = rand(0, Math.PI * 2)
        const speed = rand(2, 6)
        particles.push({
          x: fx, y: fy,
          vx: Math.cos(angle) * speed,
          vy: Math.sin(angle) * speed,
          life: 1,
          color,
          size: rand(2, 5),
        })
      }
      g.fireworks.push({ x: fx, y: fy, particles })
    }

    function spawnObstacle() {
      const type: Obstacle['type'] = Math.random() < 0.5 ? 'rock' : 'gap'
      const width = type === 'rock' ? rand(24, 44) : rand(45, 75)
      const height = type === 'rock' ? rand(22, 38) : 0
      g.obstacles.push({
        worldX: g.nextObstacleWorldX,
        type, width, height,
        passed: false,
      })
      g.nextObstacleWorldX += OBSTACLE_INTERVAL + rand(-80, 80)
    }

    function update() {
      if (g.status === 'victory') {
        g.fwTimer++
        if (g.fwTimer > 30) {
          g.fwTimer = 0
          spawnFirework()
        }
        for (const fw of g.fireworks) {
          for (const p of fw.particles) {
            p.x += p.vx
            p.y += p.vy
            p.vy += 0.05
            p.life -= 0.02
          }
          fw.particles = fw.particles.filter(p => p.life > 0)
        }
        g.fireworks = g.fireworks.filter(fw => fw.particles.length > 0)
        return
      }

      if (g.keys.has('ArrowUp') || g.keys.has('w')) {
        g.bikeVX = Math.min(g.bikeVX + ACCEL, MAX_SPEED)
      }
      if (g.keys.has('ArrowDown') || g.keys.has('s')) {
        g.bikeVX = Math.max(g.bikeVX - BRAKE, 0)
      }
      if (g.keys.has('ArrowLeft') || g.keys.has('a')) {
        g.rotation -= 0.04
      }
      if (g.keys.has('ArrowRight') || g.keys.has('d')) {
        g.rotation += 0.04
      }
      const spaceDown = g.keys.has(' ')
      if (spaceDown && g.onGround && !g.spacePressed) {
        g.bikeVY = JUMP_FORCE
        g.onGround = false
        g.spacePressed = true
      }
      if (!spaceDown) {
        g.spacePressed = false
      }

      g.worldOffset += g.bikeVX * 1.2
      const bikeWorldX = g.worldOffset + BIKE_SCREEN_X

      for (const obs of g.obstacles) {
        if (!obs.passed && obs.worldX + obs.width / 2 < bikeWorldX) {
          obs.passed = true
          g.score++
          if (g.score >= 20) {
            g.status = 'victory'
            return
          }
        }
      }

      g.obstacles = g.obstacles.filter(o => o.worldX - o.width / 2 > g.worldOffset - 200)

      while (g.nextObstacleWorldX < g.worldOffset + CW + 300) {
        spawnObstacle()
      }

      let overGap = false
      for (const obs of g.obstacles) {
        if (obs.type === 'gap') {
          const gapL = obs.worldX - obs.width / 2
          const gapR = obs.worldX + obs.width / 2
          if (bikeWorldX >= gapL && bikeWorldX <= gapR) {
            overGap = true
            break
          }
        }
      }

      if (overGap) {
        g.bikeVY += GRAVITY
        g.bikeY += g.bikeVY
        g.onGround = false
      } else {
        const targetY = terrainHeight(bikeWorldX)
        if (g.onGround) {
          g.bikeY = targetY
          g.bikeVY = 0
          g.rotation = lerp(g.rotation, terrainAngle(bikeWorldX), 0.1)
        } else {
          g.bikeVY += GRAVITY
          g.bikeY += g.bikeVY
          if (g.bikeY >= targetY) {
            g.bikeY = targetY
            g.bikeVY = 0
            g.onGround = true
          }
        }
      }

      g.rotation = Math.max(-0.6, Math.min(0.6, g.rotation))

      for (const obs of g.obstacles) {
        if (obs.type === 'rock') {
          const sx = obs.worldX - g.worldOffset
          const rockL = sx - obs.width / 2
          const rockR = sx + obs.width / 2
          const rockB = terrainHeight(obs.worldX)
          const rockT = rockB - obs.height

          const bikeL = BIKE_SCREEN_X - BIKE_W / 2
          const bikeR = BIKE_SCREEN_X + BIKE_W / 2
          const bikeT = g.bikeY - BIKE_H
          const bikeB = g.bikeY

          if (bikeR > rockL && bikeL < rockR && bikeB > rockT && bikeT < rockB) {
            g.status = 'gameover'
            return
          }
        }
      }

      if (g.bikeY > CH + 60) {
        g.status = 'gameover'
      }
    }

    function draw() {
      if (!ctx) return
      ctx.clearRect(0, 0, CW, CH)

      const grad = ctx.createLinearGradient(0, 0, 0, CH)
      grad.addColorStop(0, '#0f0f2e')
      grad.addColorStop(0.35, '#1e2a5e')
      grad.addColorStop(0.65, '#4a6a9e')
      grad.addColorStop(1, '#7a9abe')
      ctx.fillStyle = grad
      ctx.fillRect(0, 0, CW, CH)

      ctx.fillStyle = 'rgba(255,255,255,0.35)'
      for (let i = 0; i < 25; i++) {
        const sx = (i * 137 + 50) % CW
        const sy = (i * 89 + 20) % (CH * 0.35)
        const sz = (i % 3) + 1
        ctx.fillRect(sx, sy, sz, sz)
      }

      const moonX = CW - 80
      const moonY = 50
      ctx.beginPath()
      ctx.arc(moonX, moonY, 22, 0, Math.PI * 2)
      ctx.fillStyle = '#ddddaa'
      ctx.fill()
      ctx.beginPath()
      ctx.arc(moonX + 8, moonY - 5, 18, 0, Math.PI * 2)
      ctx.fillStyle = '#0f0f2e'
      ctx.fill()

      ctx.beginPath()
      ctx.moveTo(0, CH)
      for (let sx = 0; sx <= CW; sx += 3) {
        const wx = g.worldOffset + sx
        ctx.lineTo(sx, terrainHeight(wx))
      }
      ctx.lineTo(CW, CH)
      ctx.closePath()
      const groundGrad = ctx.createLinearGradient(0, CH - 80, 0, CH)
      groundGrad.addColorStop(0, '#4a6a2a')
      groundGrad.addColorStop(0.2, '#3a5a1a')
      groundGrad.addColorStop(0.6, '#2a3a0a')
      groundGrad.addColorStop(1, '#1a2a00')
      ctx.fillStyle = groundGrad
      ctx.fill()

      ctx.beginPath()
      for (let sx = 0; sx <= CW; sx += 2) {
        const wx = g.worldOffset + sx
        const h = terrainHeight(wx)
        if (sx === 0) ctx.moveTo(sx, h)
        else ctx.lineTo(sx, h)
      }
      ctx.strokeStyle = '#5a8a3a'
      ctx.lineWidth = 3
      ctx.stroke()

      for (const obs of g.obstacles) {
        const sx = obs.worldX - g.worldOffset
        if (sx < -150 || sx > CW + 150) continue

        if (obs.type === 'rock') {
          const rockY = terrainHeight(obs.worldX)
          ctx.fillStyle = '#5a4a3a'
          ctx.fillRect(sx - obs.width / 2, rockY - obs.height, obs.width, obs.height)
          ctx.fillStyle = '#7a6a5a'
          ctx.fillRect(sx - obs.width / 2 + 2, rockY - obs.height + 2, obs.width * 0.35, 4)
          ctx.fillStyle = '#4a3a2a'
          ctx.fillRect(sx - obs.width / 2, rockY - obs.height, 3, obs.height)
          ctx.strokeStyle = '#3a2a1a'
          ctx.lineWidth = 1.5
          ctx.strokeRect(sx - obs.width / 2, rockY - obs.height, obs.width, obs.height)
        }
      }

      for (const obs of g.obstacles) {
        if (obs.type === 'gap') {
          const sx = obs.worldX - g.worldOffset
          const gapL = sx - obs.width / 2
          const gapR = sx + obs.width / 2
          const edgeY = terrainHeight(obs.worldX - obs.width / 2)

          const pitGrad = ctx.createLinearGradient(0, edgeY, 0, CH)
          pitGrad.addColorStop(0, '#1a0a00')
          pitGrad.addColorStop(0.3, '#0d0500')
          pitGrad.addColorStop(1, '#000')
          ctx.fillStyle = pitGrad
          ctx.fillRect(gapL, edgeY, obs.width, CH - edgeY)

          ctx.fillStyle = '#c0392b'
          ctx.fillRect(gapL, edgeY - 2, 4, CH - edgeY + 2)
          ctx.fillRect(gapR - 4, edgeY - 2, 4, CH - edgeY + 2)

          ctx.fillStyle = '#e67e22'
          ctx.font = 'bold 11px monospace'
          ctx.textAlign = 'center'
          for (let wx = gapL + 6; wx < gapR - 6; wx += 40) {
            ctx.fillText('!PELIGRO!', wx, edgeY - 6)
          }
        }
      }

      const bx = BIKE_SCREEN_X
      const by = g.bikeY

      ctx.save()
      ctx.translate(bx, by)
      ctx.rotate(g.rotation)

      ctx.beginPath()
      ctx.arc(-BIKE_W / 2 + WHEEL_R, WHEEL_R, WHEEL_R, 0, Math.PI * 2)
      ctx.fillStyle = '#1a1a1a'
      ctx.fill()
      ctx.strokeStyle = '#444'
      ctx.lineWidth = 2
      ctx.stroke()

      ctx.beginPath()
      ctx.arc(BIKE_W / 2 - WHEEL_R, WHEEL_R, WHEEL_R, 0, Math.PI * 2)
      ctx.fillStyle = '#1a1a1a'
      ctx.fill()
      ctx.strokeStyle = '#444'
      ctx.lineWidth = 2
      ctx.stroke()

      ctx.strokeStyle = '#c0392b'
      ctx.lineWidth = 4
      ctx.lineCap = 'round'
      ctx.beginPath()
      ctx.moveTo(-BIKE_W / 2 + WHEEL_R, WHEEL_R - 1)
      ctx.lineTo(BIKE_W / 2 - WHEEL_R, WHEEL_R - 1)
      ctx.lineTo(BIKE_W / 2 - WHEEL_R, -3)
      ctx.stroke()

      ctx.strokeStyle = '#888'
      ctx.lineWidth = 2.5
      ctx.beginPath()
      ctx.moveTo(0, WHEEL_R - 1)
      ctx.lineTo(0, -BIKE_H + 4)
      ctx.stroke()

      ctx.strokeStyle = '#f1c40f'
      ctx.lineWidth = 3
      ctx.beginPath()
      ctx.moveTo(0, -BIKE_H + 4)
      ctx.lineTo(2, -BIKE_H - 8)
      ctx.stroke()

      ctx.beginPath()
      ctx.arc(2, -BIKE_H - 14, 5, 0, Math.PI * 2)
      ctx.fillStyle = '#f39c12'
      ctx.fill()
      ctx.strokeStyle = '#e67e22'
      ctx.lineWidth = 1
      ctx.stroke()

      ctx.strokeStyle = '#f1c40f'
      ctx.lineWidth = 2
      ctx.beginPath()
      ctx.moveTo(2, -BIKE_H - 7)
      ctx.lineTo(12, -BIKE_H - 1)
      ctx.stroke()
      ctx.beginPath()
      ctx.moveTo(2, -BIKE_H - 7)
      ctx.lineTo(-8, -BIKE_H - 1)
      ctx.stroke()

      ctx.restore()

      ctx.fillStyle = '#fff'
      ctx.font = 'bold 20px monospace'
      ctx.textAlign = 'left'
      ctx.fillText(`Puntaje: ${g.score}/20`, 16, 30)

      if (g.bikeVX < 0.5 && g.status === 'playing') {
        ctx.fillStyle = '#f39c12'
        ctx.font = '14px monospace'
        ctx.textAlign = 'center'
        ctx.fillText('Acelera con ↑ o W', CW / 2, CH - 30)
      }

      if (g.status === 'gameover') {
        ctx.fillStyle = 'rgba(0,0,0,0.65)'
        ctx.fillRect(0, 0, CW, CH)
        ctx.fillStyle = '#e74c3c'
        ctx.font = 'bold 48px monospace'
        ctx.textAlign = 'center'
        ctx.fillText('GAME OVER', CW / 2, CH / 2 - 25)
        ctx.fillStyle = '#fff'
        ctx.font = '24px monospace'
        ctx.fillText(`Puntaje: ${g.score}/20`, CW / 2, CH / 2 + 25)
        ctx.fillStyle = '#3498db'
        ctx.font = '18px monospace'
        ctx.fillText('ESPACIO o clic para reintentar', CW / 2, CH / 2 + 70)
      }

      if (g.status === 'victory') {
        for (const fw of g.fireworks) {
          for (const p of fw.particles) {
            ctx.globalAlpha = Math.max(0, p.life)
            ctx.fillStyle = p.color
            ctx.beginPath()
            ctx.arc(p.x, p.y, Math.max(1, p.size * p.life), 0, Math.PI * 2)
            ctx.fill()
          }
        }
        ctx.globalAlpha = 1

        ctx.fillStyle = 'rgba(0,0,0,0.3)'
        ctx.fillRect(0, 0, CW, CH)
        ctx.fillStyle = '#f1c40f'
        ctx.font = 'bold 48px monospace'
        ctx.textAlign = 'center'
        ctx.fillText('!Felicidades!', CW / 2, CH / 2 - 30)
        ctx.fillStyle = '#fff'
        ctx.font = '26px monospace'
        ctx.fillText('Puntaje maximo: 20/20', CW / 2, CH / 2 + 20)
        ctx.fillStyle = '#e74c3c'
        ctx.font = '18px monospace'
        ctx.fillText('ESPACIO o clic para jugar de nuevo', CW / 2, CH / 2 + 65)
      }

      if (g.status === 'idle') {
        ctx.fillStyle = 'rgba(0,0,0,0.55)'
        ctx.fillRect(0, 0, CW, CH)
        ctx.fillStyle = '#fff'
        ctx.font = 'bold 36px monospace'
        ctx.textAlign = 'center'
        ctx.fillText('Moto Mountain', CW / 2, CH / 2 - 50)
        ctx.fillStyle = '#bbb'
        ctx.font = '14px monospace'
        ctx.fillText('<-  -> Inclinar  |  ^ Acelerar  |  v Frenar  |  SPACE Saltar', CW / 2, CH / 2 + 5)
        ctx.fillStyle = '#f1c40f'
        ctx.font = '22px monospace'
        ctx.fillText('Presiona ESPACIO para comenzar', CW / 2, CH / 2 + 55)
      }
    }

    function loop() {
      update()
      draw()
      g.animFrameId = requestAnimationFrame(loop)
    }

    function handleKeyDown(e: KeyboardEvent) {
      const keys = ['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', ' ', 'w', 'a', 's', 'd']
      if (keys.includes(e.key)) {
        e.preventDefault()
      }

      if (e.key === ' ' || e.key === 'Space') {
        g.keys.add(' ')
        if (g.status === 'idle') {
          resetGame()
        } else if (g.status === 'gameover' || g.status === 'victory') {
          resetGame()
        }
        return
      }
      g.keys.add(e.key)

      if (g.status === 'idle' && (e.key === 'ArrowUp' || e.key === 'ArrowDown')) {
        resetGame()
      }
    }

    function handleKeyUp(e: KeyboardEvent) {
      if (e.key === ' ' || e.key === 'Space') {
        g.keys.delete(' ')
        return
      }
      g.keys.delete(e.key)
    }

    function handleClick() {
      if (g.status === 'idle' || g.status === 'gameover' || g.status === 'victory') {
        resetGame()
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    window.addEventListener('keyup', handleKeyUp)
    canvas.addEventListener('click', handleClick)

    g.animFrameId = requestAnimationFrame(loop)

    return () => {
      window.removeEventListener('keydown', handleKeyDown)
      window.removeEventListener('keyup', handleKeyUp)
      canvas.removeEventListener('click', handleClick)
      cancelAnimationFrame(g.animFrameId)
    }
  }, [resetGame])

  return (
    <div className={`flex justify-center ${className ?? ''}`}>
      <canvas
        ref={canvasRef}
        width={CW}
        height={CH}
        className="w-full max-w-[800px] h-auto rounded-xl border border-border/50 shadow-2xl cursor-pointer"
        style={{ aspectRatio: `${CW}/${CH}` }}
      />
    </div>
  )
}
