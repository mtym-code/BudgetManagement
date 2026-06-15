using System;
using System.Collections.Generic;
using System.Text;

namespace BudgetManagement.Models
{
    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }     // ⭕ ? を追加
        public string? Password { get; set; } // ⭕ ? を追加
        public string? Role { get; set; }     // ⭕ ? を追加
        /// <summary>
        /// M_TANTOマスタのSESIKMEI（正式名・全角文字列）
        /// </summary>
        public string? Sesikmei { get; set; } // 追加
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
