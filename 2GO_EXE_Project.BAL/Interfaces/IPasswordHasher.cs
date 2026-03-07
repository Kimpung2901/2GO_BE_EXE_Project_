namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IPasswordHasher
{
    string HashPassword(string password, out string salt);
    bool VerifyPassword(string password, string storedHash, string storedSalt);
}
