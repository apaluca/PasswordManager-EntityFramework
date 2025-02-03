using PasswordManager.Core.Models;
using PasswordManager.Core.Services.Interfaces;
using PasswordManager.Data.Context;
using PasswordManager.Data.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Text;
using System.Threading.Tasks;

namespace PasswordManager.Data.Repositories
{
        public class StoredPasswordRepository : IStoredPasswordRepository
        {
                private readonly PasswordManagerEntities _context;
                private readonly IEncryptionService _encryptionService;
                private readonly ISecurityManagementRepository _securityRepository;

                public StoredPasswordRepository(PasswordManagerEntities context, IEncryptionService encryptionService, ISecurityManagementRepository securityRepository)
                {
                        _context = context;
                        _encryptionService = encryptionService;
                        _securityRepository = securityRepository;
                }

                public StoredPasswordModel GetById(int passwordId)
                {
                        var query = from p in _context.StoredPasswords
                                    where p.PasswordId == passwordId
                                    select new StoredPasswordModel
                                    {
                                            Id = p.PasswordId,
                                            UserId = p.UserId,
                                            GroupId = p.GroupId,
                                            SiteName = p.SiteName,
                                            SiteUrl = p.SiteUrl,
                                            Username = p.Username,
                                            EncryptedPassword = p.EncryptedPassword,
                                            Notes = p.Notes,
                                            CreatedDate = p.CreatedDate,
                                            ModifiedDate = p.ModifiedDate,
                                            ExpirationDate = p.ExpirationDate,
                                            GroupName = p.PasswordGroup.GroupName
                                    };

                        return query.FirstOrDefault();
                }

                public IEnumerable<StoredPasswordModel> GetByUserId(int userId)
                {
                        var query = from p in _context.StoredPasswords
                                    where p.UserId == userId
                                    select new StoredPasswordModel
                                    {
                                            Id = p.PasswordId,
                                            UserId = p.UserId,
                                            GroupId = p.GroupId,
                                            SiteName = p.SiteName,
                                            SiteUrl = p.SiteUrl,
                                            Username = p.Username,
                                            EncryptedPassword = p.EncryptedPassword,
                                            Notes = p.Notes,
                                            CreatedDate = p.CreatedDate,
                                            ModifiedDate = p.ModifiedDate,
                                            ExpirationDate = p.ExpirationDate,
                                            GroupName = p.PasswordGroup.GroupName
                                    };

                        return query.ToList();
                }

                public IEnumerable<StoredPasswordModel> GetByGroup(int groupId)
                {
                        var query = from p in _context.StoredPasswords
                                    where p.GroupId == groupId
                                    select new StoredPasswordModel
                                    {
                                            Id = p.PasswordId,
                                            UserId = p.UserId,
                                            GroupId = p.GroupId,
                                            SiteName = p.SiteName,
                                            SiteUrl = p.SiteUrl,
                                            Username = p.Username,
                                            EncryptedPassword = p.EncryptedPassword,
                                            Notes = p.Notes,
                                            CreatedDate = p.CreatedDate,
                                            ModifiedDate = p.ModifiedDate,
                                            ExpirationDate = p.ExpirationDate,
                                            GroupName = p.PasswordGroup.GroupName
                                    };

                        return query.ToList();
                }

                public void Create(StoredPasswordModel password)
                {
                        var entity = new StoredPassword
                        {
                                UserId = password.UserId,
                                GroupId = password.GroupId,
                                SiteName = password.SiteName,
                                SiteUrl = password.SiteUrl,
                                Username = password.Username,
                                EncryptedPassword = password.EncryptedPassword,
                                Notes = password.Notes,
                                CreatedDate = DateTime.Now,
                                ModifiedDate = DateTime.Now,
                                ExpirationDate = password.ExpirationDate
                        };

                        _context.StoredPasswords.Add(entity);
                        _context.SaveChanges();
                        password.Id = entity.PasswordId;
                }

                public void Update(StoredPasswordModel password)
                {
                        var entity = _context.StoredPasswords.Find(password.Id);
                        if (entity != null)
                        {
                                entity.GroupId = password.GroupId;
                                entity.SiteName = password.SiteName;
                                entity.SiteUrl = password.SiteUrl;
                                entity.Username = password.Username;
                                entity.EncryptedPassword = password.EncryptedPassword;
                                entity.Notes = password.Notes;
                                entity.ModifiedDate = DateTime.Now;
                                entity.ExpirationDate = password.ExpirationDate;

                                _context.SaveChanges();
                        }
                }

                public void Delete(int passwordId)
                {
                        var password = _context.StoredPasswords.Find(passwordId);
                        if (password != null)
                        {
                                _context.StoredPasswords.Remove(password);
                                _context.SaveChanges();
                        }
                }

                public IEnumerable<StoredPasswordModel> Search(int userId, string searchTerm)
                {
                        var query = from p in _context.StoredPasswords
                                    where p.UserId == userId &&
                                          (p.SiteName.Contains(searchTerm) ||
                                           p.Username.Contains(searchTerm) ||
                                           p.SiteUrl.Contains(searchTerm))
                                    select new StoredPasswordModel
                                    {
                                            Id = p.PasswordId,
                                            UserId = p.UserId,
                                            GroupId = p.GroupId,
                                            SiteName = p.SiteName,
                                            SiteUrl = p.SiteUrl,
                                            Username = p.Username,
                                            EncryptedPassword = p.EncryptedPassword,
                                            Notes = p.Notes,
                                            CreatedDate = p.CreatedDate,
                                            ModifiedDate = p.ModifiedDate,
                                            ExpirationDate = p.ExpirationDate,
                                            GroupName = p.PasswordGroup.GroupName
                                    };

                        return query.ToList();
                }

                public void UpdateGroup(int passwordId, int? groupId)
                {
                        var password = _context.StoredPasswords.Find(passwordId);
                        if (password != null)
                        {
                                password.GroupId = groupId;
                                _context.SaveChanges();
                        }
                }
        }
}
