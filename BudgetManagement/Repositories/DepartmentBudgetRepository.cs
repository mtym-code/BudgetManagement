using BudgetManagement.Common.Helper;
using BudgetManagement.Models;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

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
                    m.abbreviation_name AS CompanyName  -- 修正: ryaku -> abbreviation_name
                FROM
                    ym_soshiki s
                JOIN
                    m_mei m ON s.company_code = m.name_code  -- 修正: meicd -> name_code
                WHERE
                    s.fiscal_year = @Year
                    AND s.mgmt_level = '070'
                    AND m.name_id = 'SSKIZOKU16'             -- 修正: meiid -> name_id
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
                    m.abbreviation_name AS SectionName       -- 修正: ryaku -> abbreviation_name
                FROM
                    ym_soshiki s
                INNER JOIN 
                    m_mei m ON s.section_code = m.name_code  -- 修正: meicd -> name_code
                WHERE
                    s.fiscal_year = @Year
                    AND s.company_code = @CompanyCode
                    AND s.mgmt_level = '020'
                    AND m.name_id = 'SSKIZOKU3'              -- 修正: meiid -> name_id
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
            // 素直に boolean のフラグを取得するSQLに直します
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

            // bool? で受け取り、データが無ければ false を返す
            var result = await conn.QueryFirstOrDefaultAsync<bool?>(sql, new { Year = year, CompanyCode = companyCode, SectionCode = sectionCode });
            return result ?? false;
        }

        // =========================================================
        // ④ Excel出力ボタン押下時に、予算データを取得
        // =========================================================
        public async Task<IEnumerable<ExcelBudgetData>> GetBudgetDataForExcelAsync(IDbConnection conn, string year, string companyCode, string sectionCode)
        {
            string sql = @"
                SELECT
                    y.account_code AS AccountCode,
                    y.sub_account_code AS SubAccountCode,
                    SUM(y.budget_apr) AS BudgetApr,
                    SUM(y.budget_may) AS BudgetMay,
                    SUM(y.budget_jun) AS BudgetJun,
                    SUM(y.budget_jul) AS BudgetJul,
                    SUM(y.budget_aug) AS BudgetAug,
                    SUM(y.budget_sep) AS BudgetSep,
                    SUM(y.budget_oct) AS BudgetOct,
                    SUM(y.budget_nov) AS BudgetNov,
                    SUM(y.budget_dec) AS BudgetDec,
                    SUM(y.budget_jan) AS BudgetJan,
                    SUM(y.budget_feb) AS BudgetFeb,
                    SUM(y.budget_mar) AS BudgetMar
                FROM
                    ym_soshiki s
                INNER JOIN
                    yt_yosan y
                    ON  y.fiscal_year = s.fiscal_year
                    AND y.org_code = s.section_code
                    AND y.mgmt_level = s.mgmt_level
                WHERE
                    s.fiscal_year = @Year
                    AND s.mgmt_level = '020'
                    AND s.company_code = @CompanyCode
                    AND s.section_code = @SectionCode
                    AND y.data_type = '10'
                    AND y.data_category = '1'
                GROUP BY
                    y.account_code,
                    y.sub_account_code
                ORDER BY
                    y.account_code,
                    y.sub_account_code;";

            return await conn.QueryAsync<ExcelBudgetData>(sql, new { Year = year, CompanyCode = companyCode, SectionCode = sectionCode });
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