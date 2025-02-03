using PasswordManager.Data.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PasswordManager.Data.Repositories.Interfaces
{
        public interface ILoginAttemptRepository
        {
                Task RecordAttemptAsync(string username, bool isSuccessful, string ipAddress, string userAgent);
                void RecordAttempt(string username, bool isSuccessful, string ipAddress, string userAgent);
                int GetFailedAttempts(string username, TimeSpan window);
                int GetFailedAttempts(int hours);
                IEnumerable<LoginAttempt> GetRecentAttempts(int count);
                IEnumerable<LoginAttempt> GetAttemptsByDateRange(DateTime startDate, DateTime endDate);
                DateTime? GetLastFailedAttempt(string username);
                void ClearFailedAttempts(string username);
                bool IsUserLockedOut(string username, int lockoutDurationMinutes, int maxAttempts);
                TimeSpan? GetRemainingLockoutTime(string username, int lockoutDurationMinutes, int maxAttempts);
        }
}
