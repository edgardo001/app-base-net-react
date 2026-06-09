using System.Security.Cryptography;
using AppBaseNetReact.Application.Common.Interfaces;

namespace AppBaseNetReact.Infrastructure.Services;

// RandomPasswordGenerator: produces a cryptographically random password
// for newly-created users. The plaintext is delivered once via the
// EmailConfirmation email; the user is forced to change it on first
// login via the LastPasswordChangeAt == null invariant (see
// User.IsPasswordExpired and User.ForcePasswordChange).
//
// Algorithm:
//   1. Fill `length` positions with uniform random chars from
//      [A-Za-z0-9] (62 chars) via RandomNumberGenerator.GetInt32,
//      which is documented to be uniform and free of modulo bias
//      for any toExclusive in [1, 2^31-1].
//   2. Enforce at least one uppercase, one lowercase, one digit
//      by overwriting random positions with chars from the
//      appropriate subset.
//   3. Return the buffer as a string.
public sealed class RandomPasswordGenerator : IRandomPasswordGenerator
{
    private const string AllChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private const string UpperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string LowerChars = "abcdefghijklmnopqrstuvwxyz";
    private const string DigitChars = "0123456789";

    public string Generate(int length = 12)
    {
        if (length < 3)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be at least 3 to satisfy the policy (uppercase + lowercase + digit).");

        var buffer = new char[length];
        for (var i = 0; i < length; i++)
            buffer[i] = AllChars[RandomNumberGenerator.GetInt32(AllChars.Length)];

        // Overwrite three distinct random positions with each required
        // class to guarantee the policy without reducing overall entropy.
        // Using a shuffle of indices [0..length-1) ensures no two
        // required-class writes land on the same position.
        Span<int> indices = stackalloc int[length];
        for (var i = 0; i < length; i++) indices[i] = i;
        // Fisher-Yates partial shuffle for first 3 elements
        for (var i = 0; i < 3; i++)
        {
            var j = RandomNumberGenerator.GetInt32(i, length);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }
        buffer[indices[0]] = UpperChars[RandomNumberGenerator.GetInt32(UpperChars.Length)];
        buffer[indices[1]] = LowerChars[RandomNumberGenerator.GetInt32(LowerChars.Length)];
        buffer[indices[2]] = DigitChars[RandomNumberGenerator.GetInt32(DigitChars.Length)];

        return new string(buffer);
    }
}
