namespace DigitalBookLibrary.Domain.Entities
{



    public class UserRole
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }

        public UserAccount? User { get; set; }
        public Role? Role { get; set; }
    }
}