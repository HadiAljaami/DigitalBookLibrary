namespace DigitalBookLibrary.Domain.Errors
{
    /// <summary>Category errors. See docs/06-Error-Codes.md §5.</summary>
    public static class CategoryErrors
    {
        public static readonly Error NotFound =
            new("CATEGORY_NOT_FOUND", "No category exists with the supplied identifier.");

        public static readonly Error NameRequired =
            new("CATEGORY_NAME_REQUIRED", "The category name is required.");

        public static readonly Error HasBooks =
            new("CATEGORY_HAS_BOOKS", "Cannot delete a category that still has books.");

        public static readonly Error HasChildren =
            new("CATEGORY_HAS_CHILDREN", "Cannot delete a category that has child categories.");

        public static readonly Error ParentInvalid =
            new("CATEGORY_PARENT_INVALID", "The parent category id is invalid or would create a cycle.");
    }
}
