using PasswordManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PasswordManager.Data.Repositories.Interfaces
{
        public interface ISecurityManagementRepository
        {
                // Password Policy
                PasswordPolicyModel GetActivePolicy();
                void CreatePolicy(PasswordPolicyModel policy);
                void UpdatePolicy(PasswordPolicyModel policy);
                void DeactivatePolicy(int policyId);
                IEnumerable<PasswordPolicyModel> GetPolicyHistory();

                // Security Alerts
                void CreateAlert(SecurityAlertModel alert);
                void UpdateAlert(SecurityAlertModel alert);
                void ResolveAlert(int alertId, int resolvedByUserId, string notes);
                IEnumerable<SecurityAlertModel> GetActiveAlerts();
                IEnumerable<SecurityAlertModel> GetAlertsByDateRange(DateTime start, DateTime end);
                SecurityAlertModel GetAlertById(int alertId);
                int GetUnresolvedAlertCount();

                // IP Blocking
                void BlockIP(BlockedIPModel blockedIP);
                void UnblockIP(int blockId);
                bool IsIPBlocked(string ipAddress);
                IEnumerable<BlockedIPModel> GetActiveBlocks();
                IEnumerable<BlockedIPModel> GetBlockHistory();
                BlockedIPModel GetBlockById(int blockId);
                void CleanupExpiredBlocks();

                // Security Metrics
                SecurityMetricsModel GetSecurityMetrics();

                // Automated Security Methods
                void CheckForSuspiciousActivity();
                void LogSecurityAction(int? userId, string action, string details, string ipAddress);
        }
}
