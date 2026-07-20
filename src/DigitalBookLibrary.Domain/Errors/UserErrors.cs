namespace DigitalBookLibrary.Domain.Errors
{
    /// <summary>User/account errors. See docs/06-Error-Codes.md §2.</summary>
    public static class UserErrors
    {
        public static readonly Error NotFound =
            new("USER_NOT_FOUND", "No user exists with the given id/email.");

        public static readonly Error EmailInUse =
            new("USER_EMAIL_IN_USE", "The email address is already registered.");

        public static readonly Error UsernameInUse =
            new("USER_USERNAME_IN_USE", "The username is already taken.");

        public static readonly Error Inactive =
            new("USER_INACTIVE", "The account is deactivated (IsActive = false).");

        public static readonly Error CannotModifySelf =
            new("USER_CANNOT_MODIFY_SELF",
                "An admin cannot deactivate their own account or remove their own Admin role.");

        public static readonly Error RoleInvalid =
            new("USER_ROLE_INVALID", "One or more of the given role names do not exist.");

        public static readonly Error LastAdmin =
            new("USER_LAST_ADMIN", "The last remaining Admin cannot be demoted or deactivated.");
    }
}
