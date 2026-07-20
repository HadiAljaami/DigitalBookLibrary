namespace DigitalBookLibrary.Application.Interfaces
{
    /// <summary>Hashes and verifies user passwords. Implemented in Infrastructure.</summary>
    public interface IPasswordHasher
    {
        string Hash(string password);

        bool Verify(string password, string passwordHash);
    }
}
