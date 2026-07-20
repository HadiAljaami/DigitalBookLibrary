using DigitalBookLibrary.Application.DTOs.Authors;
using DigitalBookLibrary.Domain.Entities;

namespace DigitalBookLibrary.Application.Mapping
{
    /// <summary>Manual entity → DTO mapping for authors (no AutoMapper).</summary>
    public static class AuthorMappings
    {
        public static AuthorListDto ToListDto(this Author author) => new()
        {
            Id = author.Id,
            FullName = author.Person?.FullName ?? string.Empty,
            Nationality = author.Person?.Nationality,
            ImageUrl = author.Person?.ImageUrl,
            IsVisible = author.IsVisible,
            HasAccount = author.Person?.UserAccount is not null
        };

        public static AuthorDetailsDto ToDetailsDto(this Author author) => new()
        {
            Id = author.Id,
            FullName = author.Person?.FullName ?? string.Empty,
            Bio = author.Person?.Bio,
            BirthDate = author.Person?.BirthDate,
            Nationality = author.Person?.Nationality,
            City = author.Person?.City,
            Country = author.Person?.Country,
            ImageUrl = author.Person?.ImageUrl,
            IsVisible = author.IsVisible,
            HasAccount = author.Person?.UserAccount is not null,
            Books = author.Books.Select(b => b.ToListDto()).ToList()
        };

        /// <summary>Applies the editable person fields onto the author's Person record.</summary>
        public static void ApplyFrom(this Person person, SaveAuthorDto dto)
        {
            person.FullName = dto.FullName.Trim();
            person.Bio = dto.Bio;
            person.BirthDate = dto.BirthDate;
            person.Nationality = dto.Nationality;
            person.City = dto.City;
            person.Country = dto.Country;
            person.ImageUrl = dto.ImageUrl;
        }
    }
}
