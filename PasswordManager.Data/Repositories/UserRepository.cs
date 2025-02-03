using PasswordManager.Core.Services.Interfaces;
using PasswordManager.Data.Context;
using PasswordManager.Data.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PasswordManager.Data.Repositories
{
        public class UserRepository : IUserRepository
        {
                private readonly PasswordManagerEntities _context;
                private readonly ISecurityService _securityService;

                public UserRepository(PasswordManagerEntities context, ISecurityService securityService)
                {
                        _context = context;
                        _securityService = securityService;
                }

                public User GetById(int userId)
                {
                        return _context.Users
                            .Include(u => u.Role)
                            .FirstOrDefault(u => u.UserId == userId && (u.IsActive ?? true));
                }

                public User GetByUsername(string username)
                {
                        return _context.Users
                            .Include(u => u.Role)
                            .FirstOrDefault(u => u.Username == username && (u.IsActive ?? true));
                }

                public bool ValidateUser(string username, string password)
                {
                        var user = GetByUsername(username);
                        if (user == null) return false;

                        return _securityService.VerifyPassword(password, user.PasswordHash);
                }

                public void Create(string username, string password, string email, int roleId)
                {
                        var user = new User
                        {
                                Username = username,
                                PasswordHash = _securityService.HashPassword(password),
                                Email = email,
                                RoleId = roleId,
                                CreatedDate = DateTime.Now,
                                IsActive = true
                        };

                        _context.Users.Add(user);
                        _context.SaveChanges();
                }

                public void Update(User user)
                {
                        _context.Entry(user).State = EntityState.Modified;
                        _context.SaveChanges();
                }

                public void ChangePassword(int userId, string newPassword)
                {
                        var user = GetById(userId);
                        if (user != null)
                        {
                                user.PasswordHash = _securityService.HashPassword(newPassword);
                                _context.SaveChanges();
                        }
                }

                public void EnableTwoFactor(int userId, string secretKey)
                {
                        var user = GetById(userId);
                        if (user != null)
                        {
                                user.TwoFactorEnabled = true;
                                user.TwoFactorSecret = secretKey;
                                _context.SaveChanges();
                        }
                }

                public void DisableTwoFactor(int userId)
                {
                        var user = GetById(userId);
                        if (user != null)
                        {
                                user.TwoFactorEnabled = false;
                                user.TwoFactorSecret = null;
                                _context.SaveChanges();
                        }
                }

                public void SetLastLoginDate(int userId)
                {
                        var user = GetById(userId);
                        if (user != null)
                        {
                                user.LastLoginDate = DateTime.Now;
                                _context.SaveChanges();
                        }
                }

                public bool IsUsernameTaken(string username)
                {
                        return _context.Users.Any(u => u.Username == username);
                }

                public bool IsEmailTaken(string email)
                {
                        return _context.Users.Any(u => u.Email == email);
                }

                public IEnumerable<User> GetUsers()
                {
                        return _context.Users
                            .Include(u => u.Role)
                            .AsNoTracking()
                            .ToList();
                }

                public void UpdateUserStatus(int userId, bool isActive)
                {
                        var user = _context.Users.Find(userId);
                        if (user != null)
                        {
                                user.IsActive = isActive;
                                _context.SaveChanges();
                        }
                }

                public IEnumerable<Role> GetAllRoles()
                {
                        return _context.Roles.ToList();
                }
        }
}
