using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Collections.Generic;

using entobel_be.Models;

namespace entobel_be.Services
{
    public class UserService
    {
        private readonly DbService _dbService;
        private readonly string secretKey = "your-secret-key";

        public UserService(DbService dbService)
        {
            _dbService = dbService;
        }

        public string Login(string username, string password)
        {
            var users = _dbService.GetUsers();
            var user = users.FirstOrDefault(u => u.Username == username);

            if (user != null)
            {
                bool compareResult = JwtService.HashCompare(password, user.Password);
                System.Diagnostics.Debug.WriteLine(compareResult);
                System.Diagnostics.Debug.WriteLine("dsfdsfdsfsf");

                if (compareResult)
                {
                    var token = JwtService.SignToken(username, secretKey);
                    System.Diagnostics.Debug.WriteLine(token);
                    return token;
                }
            }

            throw new UnauthorizedAccessException("Invalid username or password");
        }

        public bool Auth(string authHeader)
        {
            JwtService.VerifyToken(authHeader, secretKey);
            return true;
        }

        public void InitUsers()
        {
            var users = _dbService.GetUsers();

            if (users.Count == 0)
            {
                var newUsers = new List<User>
            {
                new User
                {
                    Username = "administrator",
                    Password = "$2b$10$KVQyl7bak9pyNKLBFSzeWe9NIQiHYf2qtntTx5C8p5qEEdY1.s4E2"
                },
                new User
                {
                    Username = "user",
                    Password = "$2b$10$05CeurrNTx0KCN.49APa8uf5KWoxdWaa/H.5iqf5lo.Nf8CAGzW1m"
                }
            };

                foreach (var user in newUsers)
                {
                    //await _dbService.CreateUser(user);
                }

                Console.WriteLine("Default users created successfully");
            }
            else
            {
                Console.WriteLine("Default users already exist");
            }
        }

        //private async Task CreateUser(User user)
        //{
        //    // Create and save user
        //}

        //private async Task<List<User>> GetUsers()
        //{
        //    var users = await _userCollection.Find(_ => true).ToListAsync()
        //    return users;
        //}
    }
}
