using PasswordManager.Core.Models;
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
        public class PasswordGroupRepository : IPasswordGroupRepository
        {
                private readonly PasswordManagerEntities _context;

                public PasswordGroupRepository(PasswordManagerEntities context)
                {
                        _context = context;
                }

                public PasswordGroupModel GetById(int groupId)
                {
                        var query = from g in _context.PasswordGroups
                                    where g.GroupId == groupId
                                    select new PasswordGroupModel
                                    {
                                            GroupId = g.GroupId,
                                            UserId = g.UserId,
                                            GroupName = g.GroupName,
                                            Description = g.Description,
                                            CreatedDate = g.CreatedDate,
                                            ModifiedDate = g.ModifiedDate,
                                    };

                        var group = query.FirstOrDefault();
                        if (group != null)
                        {
                                // Load passwords separately to avoid mapping issues
                                group.Passwords = GetPasswordsForGroup(groupId);
                        }
                        return group;
                }

                private ICollection<StoredPasswordModel> GetPasswordsForGroup(int groupId)
                {
                        return _context.StoredPasswords
                                .Where(p => p.GroupId == groupId)
                                .Select(p => new StoredPasswordModel
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
                                        ExpirationDate = p.ExpirationDate
                                })
                                .ToList();
                }

                public IEnumerable<PasswordGroupModel> GetByUserId(int userId)
                {
                        var query = from g in _context.PasswordGroups
                                    where g.UserId == userId
                                    select new PasswordGroupModel
                                    {
                                            GroupId = g.GroupId,
                                            UserId = g.UserId,
                                            GroupName = g.GroupName,
                                            Description = g.Description,
                                            CreatedDate = g.CreatedDate,
                                            ModifiedDate = g.ModifiedDate
                                    };

                        var groups = query.ToList();
                        foreach (var group in groups)
                        {
                                group.Passwords = GetPasswordsForGroup(group.GroupId);
                        }
                        return groups;
                }

                public void Create(PasswordGroupModel group)
                {
                        var entity = new PasswordGroup
                        {
                                UserId = group.UserId,
                                GroupName = group.GroupName,
                                Description = group.Description,
                                CreatedDate = DateTime.Now,
                                ModifiedDate = DateTime.Now
                        };

                        _context.PasswordGroups.Add(entity);
                        _context.SaveChanges();
                        group.GroupId = entity.GroupId;
                }

                public void Update(PasswordGroupModel group)
                {
                        var entity = _context.PasswordGroups.Find(group.GroupId);
                        if (entity != null)
                        {
                                entity.GroupName = group.GroupName;
                                entity.Description = group.Description;
                                entity.ModifiedDate = DateTime.Now;

                                _context.SaveChanges();
                        }
                }

                public void Delete(int groupId)
                {
                        var group = _context.PasswordGroups.Find(groupId);
                        if (group != null)
                        {
                                // Remove group association from passwords
                                var passwords = _context.StoredPasswords.Where(p => p.GroupId == groupId);
                                foreach (var password in passwords)
                                {
                                        password.GroupId = null;
                                }

                                _context.PasswordGroups.Remove(group);
                                _context.SaveChanges();
                        }
                }

                public bool IsGroupNameTaken(int userId, string groupName)
                {
                        return _context.PasswordGroups.Any(g =>
                            g.UserId == userId &&
                            g.GroupName.ToLower() == groupName.ToLower());
                }

                public int GetPasswordCount(int groupId)
                {
                        return _context.StoredPasswords.Count(p => p.GroupId == groupId);
                }

                private PasswordGroupModel MapToModel(PasswordGroup entity)
                {
                        return new PasswordGroupModel
                        {
                                GroupId = entity.GroupId,
                                UserId = entity.UserId,
                                GroupName = entity.GroupName,
                                Description = entity.Description,
                                CreatedDate = entity.CreatedDate,
                                ModifiedDate = entity.ModifiedDate,
                                Passwords = entity.StoredPasswords
                                .Select(p => new StoredPasswordModel
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
                                        ExpirationDate = p.ExpirationDate
                                })
                                .ToList()
                        };
                }
        }
}
