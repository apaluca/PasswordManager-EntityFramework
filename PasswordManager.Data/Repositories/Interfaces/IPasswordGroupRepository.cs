using PasswordManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PasswordManager.Data.Repositories.Interfaces
{
        public interface IPasswordGroupRepository
        {
                PasswordGroupModel GetById(int groupId);
                IEnumerable<PasswordGroupModel> GetByUserId(int userId);
                void Create(PasswordGroupModel group);
                void Update(PasswordGroupModel group);
                void Delete(int groupId);
                bool IsGroupNameTaken(int userId, string groupName);
                int GetPasswordCount(int groupId);
        }
}
