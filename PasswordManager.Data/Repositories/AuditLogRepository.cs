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
        public class AuditLogRepository : IAuditLogRepository
        {
                private readonly PasswordManagerEntities _context;

                public AuditLogRepository(PasswordManagerEntities context)
                {
                        _context = context;
                }

                public void LogAction(int? userId, string action, string details, string ipAddress)
                {
                        var log = new AuditLog
                        {
                                UserId = userId,
                                Action = action,
                                Details = details,
                                IPAddress = ipAddress,
                                ActionDate = DateTime.Now
                        };

                        _context.AuditLogs.Add(log);
                        _context.SaveChanges();
                }

                public IEnumerable<AuditLog> GetSystemActivityLogs(int count)
                {
                        return _context.AuditLogs
                            .Include(l => l.User)
                            .Where(l =>
                                l.Action.Contains("User") ||
                                l.Action.Contains("Role") ||
                                l.Action.Contains("Password") ||
                                l.Action.Contains("2FA"))
                            .OrderByDescending(l => l.ActionDate)
                            .Take(count)
                            .ToList();
                }

                public IEnumerable<AuditLog> GetSecurityActivityLogs(int count)
                {
                        return _context.AuditLogs
                            .Include(l => l.User)
                            .Where(l =>
                                l.Action.Contains("Login") ||
                                l.Action.Contains("Failed") ||
                                l.Action.Contains("Access") ||
                                l.Action.Contains("Security"))
                            .OrderByDescending(l => l.ActionDate)
                            .Take(count)
                            .ToList();
                }

                public IEnumerable<AuditLog> GetUserLogs(int userId)
                {
                        return _context.AuditLogs
                            .Include(l => l.User)
                            .Where(l => l.UserId == userId)
                            .OrderByDescending(l => l.ActionDate)
                            .ToList();
                }

                public IEnumerable<AuditLog> GetRecentLogs(int count)
                {
                        return _context.AuditLogs
                            .Include(l => l.User)
                            .OrderByDescending(l => l.ActionDate)
                            .Take(count)
                            .ToList();
                }

                public IEnumerable<AuditLog> GetLogsByDateRange(DateTime start, DateTime end)
                {
                        return _context.AuditLogs
                            .Include(l => l.User)
                            .Where(l => l.ActionDate >= start && l.ActionDate <= end)
                            .OrderByDescending(l => l.ActionDate)
                            .ToList();
                }
        }
}
