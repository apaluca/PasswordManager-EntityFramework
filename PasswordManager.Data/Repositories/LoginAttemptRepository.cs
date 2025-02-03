using PasswordManager.Data.Context;
using PasswordManager.Data.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PasswordManager.Data.Repositories
{
        public class LoginAttemptRepository : ILoginAttemptRepository
        {
                private readonly PasswordManagerEntities _context;

                public LoginAttemptRepository(PasswordManagerEntities context)
                {
                        _context = context;
                }

                public async Task RecordAttemptAsync(string username, bool isSuccessful, string ipAddress, string userAgent)
                {
                        var attempt = new LoginAttempt
                        {
                                Username = username,
                                IsSuccessful = isSuccessful,
                                IPAddress = ipAddress,
                                UserAgent = userAgent,
                                AttemptDate = DateTime.Now
                        };

                        _context.LoginAttempts.Add(attempt);
                        await _context.SaveChangesAsync();
                }

                public void RecordAttempt(string username, bool isSuccessful, string ipAddress, string userAgent)
                {
                        var attempt = new LoginAttempt
                        {
                                Username = username,
                                IsSuccessful = isSuccessful,
                                IPAddress = ipAddress,
                                UserAgent = userAgent,
                                AttemptDate = DateTime.Now
                        };

                        _context.LoginAttempts.Add(attempt);
                        _context.SaveChanges();
                }

                public int GetFailedAttempts(string username, TimeSpan window)
                {
                        var cutoffTime = DateTime.Now.Subtract(window);
                        return _context.LoginAttempts.Count(
                            la => la.Username == username &&
                                  !la.IsSuccessful &&
                                  la.AttemptDate >= cutoffTime);
                }

                public int GetFailedAttempts(int hours)
                {
                        var cutoffTime = DateTime.Now.AddHours(-hours);
                        return _context.LoginAttempts.Count(
                            la => !la.IsSuccessful &&
                            la.AttemptDate >= cutoffTime);
                }

                public IEnumerable<LoginAttempt> GetRecentAttempts(int count)
                {
                        return _context.LoginAttempts
                                .OrderByDescending(la => la.AttemptDate)
                                .Take(count)
                                .AsNoTracking()
                                .ToList();
                }

                public IEnumerable<LoginAttempt> GetAttemptsByDateRange(DateTime startDate, DateTime endDate)
                {
                        return _context.LoginAttempts
                                .Where(la => la.AttemptDate >= startDate && la.AttemptDate <= endDate)
                                .OrderByDescending(la => la.AttemptDate)
                                .AsNoTracking()
                                .ToList();
                }

                public DateTime? GetLastFailedAttempt(string username)
                {
                        return _context.LoginAttempts
                            .Where(la => la.Username == username && !la.IsSuccessful)
                            .OrderByDescending(la => la.AttemptDate)
                            .Select(la => la.AttemptDate)
                            .FirstOrDefault();
                }

                public void ClearFailedAttempts(string username)
                {
                        var failedAttempts = _context.LoginAttempts
                            .Where(la => la.Username == username && !la.IsSuccessful);

                        foreach (var attempt in failedAttempts)
                        {
                                _context.LoginAttempts.Remove(attempt);
                        }

                        _context.SaveChanges();
                }

                public bool IsUserLockedOut(string username, int lockoutDurationMinutes, int maxAttempts)
                {
                        var lockoutWindow = TimeSpan.FromMinutes(lockoutDurationMinutes);
                        var failedAttempts = GetFailedAttempts(username, lockoutWindow);

                        if (failedAttempts >= maxAttempts)
                        {
                                var lastFailedAttempt = GetLastFailedAttempt(username);
                                if (lastFailedAttempt.HasValue)
                                {
                                        var lockoutEnd = lastFailedAttempt.Value.AddMinutes(lockoutDurationMinutes);
                                        return DateTime.Now < lockoutEnd;
                                }
                        }

                        return false;
                }

                public TimeSpan? GetRemainingLockoutTime(string username, int lockoutDurationMinutes, int maxAttempts)
                {
                        var lockoutWindow = TimeSpan.FromMinutes(lockoutDurationMinutes);
                        var failedAttempts = GetFailedAttempts(username, lockoutWindow);

                        if (failedAttempts >= maxAttempts)
                        {
                                var lastFailedAttempt = GetLastFailedAttempt(username);
                                if (lastFailedAttempt.HasValue)
                                {
                                        var lockoutEnd = lastFailedAttempt.Value.AddMinutes(lockoutDurationMinutes);
                                        if (DateTime.Now < lockoutEnd)
                                        {
                                                return lockoutEnd - DateTime.Now;
                                        }
                                }
                        }

                        return null;
                }
        }
}
