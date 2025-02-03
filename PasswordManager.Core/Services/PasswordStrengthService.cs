﻿using PasswordManager.Core.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PasswordManager.Core.Services
{
        public class PasswordStrengthService : IPasswordStrengthService
        {
                public PasswordStrength CheckStrength(string password)
                {
                        if (string.IsNullOrEmpty(password))
                                return PasswordStrength.VeryWeak;

                        int score = 0;

                        // Length checks
                        if (password.Length >= 8) score++;
                        if (password.Length >= 12) score++;

                        // Character type checks
                        if (password.Any(char.IsUpper)) score++;
                        if (password.Any(char.IsLower)) score++;
                        if (password.Any(char.IsDigit)) score++;
                        if (password.Any(c => !char.IsLetterOrDigit(c))) score++;

                        return (PasswordStrength)Math.Min(score - 1, 4);
                }

                public string GetStrengthDescription(PasswordStrength strength)
                {
                        switch (strength)
                        {
                                case PasswordStrength.VeryWeak:
                                        return "Very Weak";
                                case PasswordStrength.Weak:
                                        return "Weak";
                                case PasswordStrength.Medium:
                                        return "Medium";
                                case PasswordStrength.Strong:
                                        return "Strong";
                                case PasswordStrength.VeryStrong:
                                        return "Very Strong";
                                default:
                                        return "Unknown";
                        }
                }

                public string GetStrengthColor(PasswordStrength strength)
                {
                        switch (strength)
                        {
                                case PasswordStrength.VeryWeak:
                                        return "#FF0000"; // Red
                                case PasswordStrength.Weak:
                                        return "#FF6B6B"; // Light Red
                                case PasswordStrength.Medium:
                                        return "#FFD700"; // Yellow
                                case PasswordStrength.Strong:
                                        return "#90EE90"; // Light Green
                                case PasswordStrength.VeryStrong:
                                        return "#228B22"; // Dark Green
                                default:
                                        return "#808080"; // Gray
                        }
                }
        }
}
