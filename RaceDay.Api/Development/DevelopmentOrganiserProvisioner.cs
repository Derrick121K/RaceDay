using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Development;

internal static class DevelopmentOrganiserProvisioner
{
    public static async Task RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var passwordHasher = scope.ServiceProvider
            .GetRequiredService<IPasswordHasher<User>>();

        Console.Write("Organiser email: ");
        var email = Console.ReadLine()?.Trim().ToLowerInvariant();
        Console.Write("First name: ");
        var firstName = Console.ReadLine()?.Trim();
        Console.Write("Last name: ");
        var lastName = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(firstName) ||
            string.IsNullOrWhiteSpace(lastName))
        {
            throw new InvalidOperationException(
                "Email, first name and last name are required.");
        }

        if (!new EmailAddressAttribute().IsValid(email) ||
            email.Length > 255 ||
            firstName.Length > 50 ||
            lastName.Length > 50)
        {
            throw new InvalidOperationException(
                "Enter a valid email and names no longer than 50 characters.");
        }

        var existingUser = await db.Users
            .AnyAsync(user => user.Email == email);

        if (existingUser)
            throw new InvalidOperationException("That email already has an account.");

        var password = ReadHidden("Password (minimum 8 characters; input masked): ");
        var confirmation = ReadHidden("Confirm password: ");

        if (password.Length < 8 || password != confirmation)
            throw new InvalidOperationException("Passwords must match and be at least 8 characters.");

        var organiser = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Role = "ORGANISER",
            CreatedAt = DateTime.UtcNow
        };
        organiser.PasswordHash = passwordHasher.HashPassword(organiser, password);

        db.Users.Add(organiser);
        await db.SaveChangesAsync();

        Console.WriteLine($"Development organiser account created for {organiser.Email}.");
    }

    private static string ReadHidden(string prompt)
    {
        Console.Write(prompt);
        var characters = new List<char>();

        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return new string(characters.ToArray());
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (characters.Count > 0)
                {
                    characters.RemoveAt(characters.Count - 1);
                    Console.Write("\b \b");
                }

                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                characters.Add(key.KeyChar);
                Console.Write('*');
            }
        }
    }
}
