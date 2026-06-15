using BudgetManagement.Common.Helper;
using BudgetManagement.Models;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

// 🌟 ViewModelからアクセスできるように、Modelsとして一番外側に出しました
namespace BudgetManagement.Models
{
    public class MonthlyBudgetData
    {
        public string AccountCode { get; set; } = string.Empty;
        public string SubAccountCode { get; set; } = string.Empty;
        public decimal Month04 { get; set; }
        public decimal Month05 { get; set; }
        public decimal Month06 { get; set; }
        public decimal Month07 { get; set; }
        public decimal Month08 { get; set; }
        public decimal Month09 { get; set; }
        public decimal Month10 { get; set; }
        public decimal Month11 { get; set; }
        public decimal Month12 { get; set; }
        public decimal Month01 { get; set; }
        public decimal Month02 { get; set; }
        public decimal Month03 { get; set; }
    }
}

namespace BudgetManagement.Repositories
{
    // =========================================================
    // 🛠️ 部門別経費予算 SQL実行リポジトリクラス
    // =========================================================
    public class DepartmentBudgetRepository
    {
        public async Task<IEnumerable<DepartmentBudget>> GetAllAsync(IDbConnection conn)
        {
            var sql = SqlLoader.Load("DepartmentBudget/GetAll.sql");
            return await conn.QueryAsync<DepartmentBudget>(sql);
        }

        // =========================================================
        // ① 年度テキストボックス変更時に会社コードと名称を取得
        // =========================================================
        public async Task<IEnumerable<Company>> GetCompaniesByYearAsync(IDbConnection conn, string year)
        {
            string sql = @"
                SELECT
                    s.company_code AS CompanyCode,
                    m.abbreviation_name AS CompanyName  
                FROM
                    ym_soshiki s
                JOIN
                    m_mei m ON s.company_code = m.name_code  
                WHERE
                    s.fiscal_year = @Year
                    AND s.mgmt_level = '070'
                    AND m.name_id = 'SSKIZOKU16'             
                ORDER BY
                    s.company_code;";

            return await conn.QueryAsync<Company>(sql, new { Year = year });
        }

        // =========================================================
        // ② 年度テキストボックス変更時に課コードと名称を取得
        // =========================================================
        public async Task<IEnumerable<SectionInfo>> GetSectionsAsync(IDbConnection conn, string year, string companyCode)
        {
            string sql = @"
                SELECT
                    s.section_code AS SectionCode,
                    m.abbreviation_name AS SectionName       
                FROM
                    ym_soshiki s
                INNER JOIN 
                    m_mei m ON s.section_code = m.name_code  
                WHERE
                    s.fiscal_year = @Year
                    AND s.company_code = @CompanyCode
                    AND s.mgmt_level = '020'
                    AND m.name_id = 'SSKIZOKU3'              
                    AND s.dept_expense_budget_input_type = '1'
                ORDER BY
                    s.section_code;";

            return await conn.QueryAsync<SectionInfo>(sql, new { Year = year, CompanyCode = companyCode });
        }

        // =========================================================
        // ③ 課のコンボボックス確定時に管理入力完了フラグを取得
        // =========================================================
        public async Task<bool> GetManagementInputFlagsAsync(IDbConnection conn, string year, string companyCode, string sectionCode)
        {
            string sql = @"
                SELECT
                    u.mgmt_input_complete_flag
                FROM
                    ym_unyou u
                WHERE
                    u.input_screen = '330'
                    AND u.fiscal_year = @Year
                    AND u.company_code = @CompanyCode
                    AND u.section_code = @SectionCode
                    AND u.mgmt_input_complete_flag = true
                UNION ALL
                SELECT
                    u.staff_input_complete_flag
                FROM
                    ym_unyou u
                WHERE
                    u.input_screen = '360'
                    AND u.fiscal_year = @Year
                    AND u.company_code = @CompanyCode
                    AND u.staff_input_complete_flag = true;";

            var result = await conn.QueryFirstOrDefaultAsync<bool?>(sql, new { Year = year, CompanyCode = companyCode, SectionCode = sectionCode });
            return result ?? false;
        }

        // =========================================================
        // ④ Excel出力ボタン押下時に、予算データを取得
        // =========================================================
        public async Task<IEnumerable<MonthlyBudgetData>> GetBudgetDataForExcelAsync(IDbConnection conn, string year, string companyCode, string sectionCode)
        {
            string sql = @"
        SELECT 
            account_code AS AccountCode,
            sub_account_code AS SubAccountCode,
            SUM(CASE WHEN fiscal_month = '04' THEN budget_amount ELSE 0 END) AS Month04,
            SUM(CASE WHEN fiscal_month = '05' THEN budget_amount ELSE 0 END) AS Month05,
            SUM(CASE WHEN fiscal_month = '06' THEN budget_amount ELSE 0 END) AS Month06,
            SUM(CASE WHEN fiscal_month = '07' THEN budget_amount ELSE 0 END) AS Month07,
            SUM(CASE WHEN fiscal_month = '08' THEN budget_amount ELSE 0 END) AS Month08,
            SUM(CASE WHEN fiscal_month = '09' THEN budget_amount ELSE 0 END) AS Month09,
            SUM(CASE WHEN fiscal_month = '10' THEN budget_amount ELSE 0 END) AS Month10,
            SUM(CASE WHEN fiscal_month = '11' THEN budget_amount ELSE 0 END) AS Month11,
            SUM(CASE WHEN fiscal_month = '12' THEN budget_amount ELSE 0 END) AS Month12,
            SUM(CASE WHEN fiscal_month = '01' THEN budget_amount ELSE 0 END) AS Month01,
            SUM(CASE WHEN fiscal_month = '02' THEN budget_amount ELSE 0 END) AS Month02,
            SUM(CASE WHEN fiscal_month = '03' THEN budget_amount ELSE 0 END) AS Month03
        FROM 
            yt_yosan
        WHERE 
            fiscal_year = @Year 
            AND org_code = @SectionCode
            AND delete_flag = false
        GROUP BY 
            account_code, 
            sub_account_code;
    ";

            return await conn.QueryAsync<MonthlyBudgetData>(sql, new { Year = year, SectionCode = sectionCode });
        }

        // =========================================================
        // ⑤ 担当入力完了フラグを登録する（UPSERT）
        // =========================================================
        public async Task UpsertStaffInputStatusAsync(IDbConnection conn, string year, string companyCode, string sectionCode, string staffHandlerCode, string deleteFlag, string createdBy, DateTime createdAt, string updatedBy, DateTime updatedAt)
        {
            string sql = @"
                INSERT INTO ym_unyou (
                    input_screen, fiscal_year, company_code, dept_code, 
                    section_code, staff_code, target_month, staff_input_complete_flag, 
                    staff_handler_code, delete_flag, created_by, created_at, updated_by, updated_at
                )
                VALUES (
                    '330', @Year, @CompanyCode, '99999999', @SectionCode,
                    '99999999', 99, true, @StaffHandlerCode, @DeleteFlag::boolean,
                    @CreatedBy, @CreatedAt, @UpdatedBy, @UpdatedAt
                )
                ON CONFLICT (
                    input_screen, fiscal_year, company_code, dept_code, 
                    section_code, staff_code, target_month
                )
                DO UPDATE SET
                    staff_input_complete_flag = EXCLUDED.staff_input_complete_flag,
                    staff_handler_code        = EXCLUDED.staff_handler_code,
                    delete_flag               = EXCLUDED.delete_flag,
                    updated_by                = EXCLUDED.updated_by,
                    updated_at                = EXCLUDED.updated_at;";

            await conn.ExecuteAsync(sql, new { Year = year, CompanyCode = companyCode, SectionCode = sectionCode, StaffHandlerCode = staffHandlerCode, DeleteFlag = deleteFlag, CreatedBy = createdBy, CreatedAt = createdAt, UpdatedBy = updatedBy, UpdatedAt = updatedAt });
        }

        // =========================================================
        // ⑥ 予算データをupdateした場合、担当入力完了フラグを更新する。
        // =========================================================
        public async Task UpdateStaffInputStatusAsync(IDbConnection conn, string staffHandlerCode, string updatedBy, DateTime updatedAt, string year, string companyCode, string sectionCode)
        {
            string sql = @"
                UPDATE ym_unyou
                SET
                    staff_input_complete_flag = true,
                    staff_handler_code        = @StaffHandlerCode,
                    updated_by                = @UpdatedBy,
                    updated_at                = @UpdatedAt
                WHERE
                    input_screen  = '330'
                    AND fiscal_year  = @Year
                    AND company_code = @CompanyCode
                    AND dept_code    = '99999999'
                    AND section_code = @SectionCode
                    AND staff_code   = '99999999'
                    AND target_month = 99;";

            await conn.ExecuteAsync(sql, new { StaffHandlerCode = staffHandlerCode, UpdatedBy = updatedBy, UpdatedAt = updatedAt, Year = year, CompanyCode = companyCode, SectionCode = sectionCode });
        }
    }
}