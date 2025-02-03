using PasswordManager.Core.Models;
using PasswordManager.Data.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using PasswordManager.Data.Repositories.Interfaces;

namespace PasswordManager.Data.Repositories
{
        public class SecurityManagementRepository : ISecurityManagementRepository
        {
                private readonly PasswordManagerEntities _context;

                public SecurityManagementRepository(PasswordManagerEntities context)
                {
                        _context = context;
                }

                #region Password Policy Methods
                public PasswordPolicyModel GetActivePolicy()
                {
                        var policy = _context.PasswordPolicies
                            .Include(p => p.User)
                            .FirstOrDefault(p => p.IsActive == true);
                        return MapToPasswordPolicyModel(policy);
                }

                public void CreatePolicy(PasswordPolicyModel policy)
                {
                        // Deactivate current active policy
                        var currentActive = _context.PasswordPolicies
                            .FirstOrDefault(p => p.IsActive == true);

                        if (currentActive != null)
                        {
                                currentActive.IsActive = false;
                                _context.SaveChanges();
                        }

                        var entity = new PasswordPolicy
                        {
                                MinLength = policy.MinLength,
                                RequireUppercase = policy.RequireUppercase,
                                RequireLowercase = policy.RequireLowercase,
                                RequireNumbers = policy.RequireNumbers,
                                RequireSpecialChars = policy.RequireSpecialChars,
                                MaxLoginAttempts = policy.MaxLoginAttempts,
                                LockoutDurationMinutes = policy.LockoutDurationMinutes,
                                CreatedDate = DateTime.Now,
                                ModifiedDate = DateTime.Now,
                                ModifiedByUserId = policy.ModifiedByUserId,
                                IsActive = true
                        };

                        _context.PasswordPolicies.Add(entity);
                        _context.SaveChanges();
                }

                public void UpdatePolicy(PasswordPolicyModel policy)
                {
                        var entity = _context.PasswordPolicies.Find(policy.PolicyId);
                        if (entity != null)
                        {
                                entity.MinLength = policy.MinLength;
                                entity.RequireUppercase = policy.RequireUppercase;
                                entity.RequireLowercase = policy.RequireLowercase;
                                entity.RequireNumbers = policy.RequireNumbers;
                                entity.RequireSpecialChars = policy.RequireSpecialChars;
                                entity.MaxLoginAttempts = policy.MaxLoginAttempts;
                                entity.LockoutDurationMinutes = policy.LockoutDurationMinutes;
                                entity.ModifiedDate = DateTime.Now;
                                entity.ModifiedByUserId = policy.ModifiedByUserId;

                                _context.SaveChanges();
                        }
                }

                public void DeactivatePolicy(int policyId)
                {
                        var policy = _context.PasswordPolicies.Find(policyId);
                        if (policy != null)
                        {
                                policy.IsActive = false;
                                _context.SaveChanges();
                        }
                }

                public IEnumerable<PasswordPolicyModel> GetPolicyHistory()
                {
                        return _context.PasswordPolicies
                            .Include(p => p.User)
                            .OrderByDescending(p => p.ModifiedDate)
                            .Select(p => MapToPasswordPolicyModel(p))
                            .ToList();
                }
                #endregion

                #region Security Alert Methods
                public void CreateAlert(SecurityAlertModel alert)
                {
                        var entity = new SecurityAlert
                        {
                                AlertType = alert.AlertType.ToString(),
                                Severity = alert.Severity.ToString(),
                                Description = alert.Description,
                                Source = alert.Source,
                                IPAddress = alert.IPAddress,
                                Username = alert.Username,
                                CreatedDate = DateTime.Now,
                                IsResolved = false
                        };

                        _context.SecurityAlerts.Add(entity);
                        _context.SaveChanges();
                }

                public void UpdateAlert(SecurityAlertModel alert)
                {
                        var entity = _context.SecurityAlerts.Find(alert.AlertId);
                        if (entity != null)
                        {
                                entity.Description = alert.Description;
                                entity.Notes = alert.Notes;
                                _context.SaveChanges();
                        }
                }

                public void ResolveAlert(int alertId, int resolvedByUserId, string notes)
                {
                        var alert = _context.SecurityAlerts.Find(alertId);
                        if (alert != null)
                        {
                                alert.IsResolved = true;
                                alert.ResolvedByUserId = resolvedByUserId;
                                alert.ResolvedDate = DateTime.Now;
                                alert.Notes = notes;
                                _context.SaveChanges();
                        }
                }

                public IEnumerable<SecurityAlertModel> GetActiveAlerts()
                {
                        try
                        {
                                var alerts = _context.SecurityAlerts
                                    .Include(a => a.User)
                                    .Where(a => a.IsResolved == false || !a.IsResolved.HasValue)
                                    .OrderByDescending(a => a.CreatedDate)
                                    .ToList();

                                return alerts.Select(MapToSecurityAlertModel).ToList();
                        }
                        catch (Exception ex)
                        {
                                throw new Exception("Failed to retrieve active alerts", ex);
                        }
                }

                public SecurityAlertModel GetAlertById(int alertId)
                {
                        var alert = _context.SecurityAlerts
                            .Include(a => a.User)
                            .FirstOrDefault(a => a.AlertId == alertId);
                        return MapToSecurityAlertModel(alert);
                }

                public IEnumerable<SecurityAlertModel> GetAlertsByDateRange(DateTime start, DateTime end)
                {
                        return _context.SecurityAlerts
                            .Include(a => a.User)
                            .Where(a => a.CreatedDate >= start && a.CreatedDate <= end)
                            .OrderByDescending(a => a.CreatedDate)
                            .Select(a => MapToSecurityAlertModel(a))
                            .ToList();
                }

                public int GetUnresolvedAlertCount()
                {
                        return _context.SecurityAlerts.Count(a => a.IsResolved == false || !a.IsResolved.HasValue);
                }
                #endregion

                #region IP Blocking Methods
                public void BlockIP(BlockedIPModel blockedIP)
                {
                        var entity = new BlockedIP
                        {
                                IPAddress = blockedIP.IPAddress,
                                Reason = blockedIP.Reason,
                                BlockedDate = DateTime.Now,
                                BlockedByUserId = blockedIP.BlockedByUserId,
                                ExpiryDate = blockedIP.ExpiryDate,
                                IsActive = true,
                                Notes = blockedIP.Notes
                        };

                        _context.BlockedIPs.Add(entity);
                        _context.SaveChanges();
                }

                public void UnblockIP(int blockId)
                {
                        var block = _context.BlockedIPs.Find(blockId);
                        if (block != null)
                        {
                                block.IsActive = false;
                                block.ExpiryDate = DateTime.Now;
                                _context.SaveChanges();
                        }
                }

                public bool IsIPBlocked(string ipAddress)
                {
                        return _context.BlockedIPs.Any(b =>
                            b.IPAddress == ipAddress &&
                            b.IsActive == true &&
                            (!b.ExpiryDate.HasValue || b.ExpiryDate > DateTime.Now));
                }

                public IEnumerable<BlockedIPModel> GetActiveBlocks()
                {
                        try
                        {
                                var blocks = _context.BlockedIPs
                                    .Include(b => b.User)
                                    .Where(b => b.IsActive == true)
                                    .OrderByDescending(b => b.BlockedDate)
                                    .ToList();

                                return blocks.Select(MapToBlockedIPModel).ToList();
                        }
                        catch (Exception ex)
                        {
                                throw new Exception("Failed to retrieve active IP blocks", ex);
                        }
                }

                public BlockedIPModel GetBlockById(int blockId)
                {
                        var block = _context.BlockedIPs
                            .Include(b => b.User)
                            .FirstOrDefault(b => b.BlockId == blockId);
                        return MapToBlockedIPModel(block);
                }

                public IEnumerable<BlockedIPModel> GetBlockHistory()
                {
                        return _context.BlockedIPs
                            .Include(b => b.User)
                            .OrderByDescending(b => b.BlockedDate)
                            .Select(b => MapToBlockedIPModel(b))
                            .ToList();
                }

                public void CleanupExpiredBlocks()
                {
                        var expiredBlocks = _context.BlockedIPs
                            .Where(b => b.IsActive == true &&
                                       b.ExpiryDate.HasValue &&
                                       b.ExpiryDate.Value < DateTime.Now);

                        foreach (var block in expiredBlocks)
                        {
                                block.IsActive = false;
                        }

                        _context.SaveChanges();
                }
                #endregion

                #region Security Metrics
                public SecurityMetricsModel GetSecurityMetrics()
                {
                        var now = DateTime.Now;
                        var lastDay = now.AddDays(-1);

                        return new SecurityMetricsModel
                        {
                                TotalActiveAlerts = _context.SecurityAlerts.Count(a => a.IsResolved != true),
                                CriticalAlerts = _context.SecurityAlerts.Count(a =>
                                    a.IsResolved != true &&
                                    a.Severity == AlertSeverity.Critical.ToString()),
                                BlockedIPs = _context.BlockedIPs.Count(b => b.IsActive == true),
                                RecentFailedLogins = _context.LoginAttempts.Count(l =>
                                    l.IsSuccessful == false &&
                                    l.AttemptDate.HasValue && l.AttemptDate.Value >= lastDay),
                                ExpiredPasswords = _context.StoredPasswords.Count(p =>
                                    p.ExpirationDate.HasValue && p.ExpirationDate.Value < now),
                                LastPolicyUpdate = _context.PasswordPolicies
                                .Where(p => p.IsActive == true)
                                .Select(p => p.ModifiedDate)
                                .FirstOrDefault() ?? DateTime.MinValue,
                                AccountLockouts = _context.AuditLogs.Count(l =>
                                    l.Action == "Security_AccountLocked" &&
                                    l.ActionDate.HasValue && l.ActionDate.Value >= lastDay)
                        };
                }
                #endregion

                #region Private Mapping Methods
                private PasswordPolicyModel MapToPasswordPolicyModel(PasswordPolicy entity)
                {
                        if (entity == null) return null;

                        return new PasswordPolicyModel
                        {
                                PolicyId = entity.PolicyId,
                                MinLength = entity.MinLength,
                                RequireUppercase = entity.RequireUppercase,
                                RequireLowercase = entity.RequireLowercase,
                                RequireNumbers = entity.RequireNumbers,
                                RequireSpecialChars = entity.RequireSpecialChars,
                                MaxLoginAttempts = entity.MaxLoginAttempts,
                                LockoutDurationMinutes = entity.LockoutDurationMinutes,
                                CreatedDate = entity.CreatedDate ?? DateTime.Now,
                                ModifiedDate = entity.ModifiedDate ?? DateTime.Now,
                                ModifiedByUserId = entity.ModifiedByUserId,
                                IsActive = entity.IsActive ?? false,
                                ModifiedByUsername = entity.User?.Username
                        };
                }

                private SecurityAlertModel MapToSecurityAlertModel(SecurityAlert entity)
                {
                        if (entity == null) return null;

                        bool isResolved = entity.IsResolved.GetValueOrDefault(false);

                        return new SecurityAlertModel
                        {
                                AlertId = entity.AlertId,
                                AlertType = (AlertType)Enum.Parse(typeof(AlertType), entity.AlertType),
                                Severity = (AlertSeverity)Enum.Parse(typeof(AlertSeverity), entity.Severity),
                                Description = entity.Description,
                                Source = entity.Source,
                                IPAddress = entity.IPAddress,
                                Username = entity.Username,
                                CreatedDate = entity.CreatedDate ?? DateTime.Now,
                                IsResolved = isResolved,
                                ResolvedByUserId = entity.ResolvedByUserId,
                                ResolvedDate = entity.ResolvedDate,
                                Notes = entity.Notes,
                                ResolvedByUsername = entity.User?.Username
                        };
                }

                private BlockedIPModel MapToBlockedIPModel(BlockedIP entity)
                {
                        if (entity == null) return null;

                        bool isActive = entity.IsActive.GetValueOrDefault(false);

                        return new BlockedIPModel
                        {
                                BlockId = entity.BlockId,
                                IPAddress = entity.IPAddress,
                                Reason = entity.Reason,
                                BlockedDate = entity.BlockedDate ?? DateTime.Now,
                                BlockedByUserId = entity.BlockedByUserId,
                                ExpiryDate = entity.ExpiryDate,
                                IsActive = isActive,
                                Notes = entity.Notes,
                                BlockedByUsername = entity.User?.Username
                        };
                }
                #endregion

                #region Automated Security Methods
                public void CheckForSuspiciousActivity()
                {
                        try
                        {
                                var lastHour = DateTime.Now.AddHours(-1);
                                var failedAttempts = _context.LoginAttempts
                                    .Where(l => l.IsSuccessful == false && l.AttemptDate >= lastHour)
                                    .GroupBy(l => new { l.IPAddress, l.Username })
                                    .Select(g => new
                                    {
                                            g.Key.IPAddress,
                                            g.Key.Username,
                                            Count = g.Count()
                                    })
                                    .Where(x => x.Count >= 5)
                                    .ToList();

                                foreach (var attempt in failedAttempts)
                                {
                                        // Create security alert
                                        var alert = new SecurityAlert
                                        {
                                                AlertType = AlertType.BruteForceAttempt.ToString(),
                                                Severity = AlertSeverity.High.ToString(),
                                                Description = $"Multiple failed login attempts ({attempt.Count}) from IP {attempt.IPAddress} for user {attempt.Username}",
                                                Source = "Security Monitor",
                                                IPAddress = attempt.IPAddress,
                                                Username = attempt.Username,
                                                CreatedDate = DateTime.Now,
                                                IsResolved = false
                                        };

                                        _context.SecurityAlerts.Add(alert);

                                        // Auto-block IP if threshold exceeded
                                        if (attempt.Count >= 10 && !IsIPBlocked(attempt.IPAddress))
                                        {
                                                var block = new BlockedIP
                                                {
                                                        IPAddress = attempt.IPAddress,
                                                        Reason = "Automated block: Multiple failed login attempts detected",
                                                        BlockedDate = DateTime.Now,
                                                        BlockedByUserId = 1, // System user ID
                                                        ExpiryDate = DateTime.Now.AddHours(24),
                                                        IsActive = true,
                                                        Notes = $"Automatically blocked after {attempt.Count} failed login attempts"
                                                };

                                                _context.BlockedIPs.Add(block);
                                        }
                                }

                                _context.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                                // Log error and continue
                                LogSecurityAction(null, "Error", $"Error in CheckForSuspiciousActivity: {ex.Message}", "System");
                        }
                }

                public void LogSecurityAction(int? userId, string action, string details, string ipAddress)
                {
                        try
                        {
                                var log = new AuditLog
                                {
                                        UserId = userId,
                                        Action = $"Security_{action}",
                                        Details = details,
                                        IPAddress = ipAddress,
                                        ActionDate = DateTime.Now
                                };

                                _context.AuditLogs.Add(log);
                                _context.SaveChanges();
                        }
                        catch
                        {
                                // Silently fail if audit logging fails
                        }
                }
                #endregion
        }
}