using BudgetManagement.Common.Helper;
using BudgetManagement.Models;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;

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
        /// <summary>
        /// 部門別経費予算の全件データを取得します（外部SQLファイルを使用）。
        /// </summary>
        public async Task<IEnumerable<DepartmentBudget>> GetAllAsync(IDbConnection conn)
        {
            var sql = SqlLoader.Load("DepartmentBudget/GetAll.sql");

            LogHelper.Debug($"[SQL Execution - GetAllAsync]\n{sql}");

            return await conn.QueryAsync<DepartmentBudget>(sql);
        }

        /// <summary>
        /// 指定された年度に該当する会社情報を取得します。
        /// </summary>
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

            var param = new { Year = year };
            LogHelper.Debug($"[SQL Execution - GetCompaniesByYearAsync]\n{sql}\nParameters: {JsonSerializer.Serialize(param)}");

            return await conn.QueryAsync<Company>(sql, param);
        }

        /// <summary>
        /// 指定された年度と会社コードに紐づく、予算入力対象の課（部門）リストを取得します。
        /// </summary>
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

            var param = new { Year = year, CompanyCode = companyCode };
            LogHelper.Debug($"[SQL Execution - GetSectionsAsync]\n{sql}\nParameters: {JsonSerializer.Serialize(param)}");

            return await conn.QueryAsync<SectionInfo>(sql, param);
        }

        /// <summary>
        /// 指定された課（部門）の予算入力完了（確定）状態を取得します。
        /// （画面330の管理完了フラグ、または画面360の担当完了フラグのいずれかが有効か判定）
        /// </summary>
        public async Task<bool> GetManagementInputFlagsAsync(IDbConnection conn, string year, string companyCode, string sectionCode)
        {
            string sql = @"
                SELECT
                    u.staff_input_complete_flag
                FROM
                    ym_unyou u
                WHERE
                    u.input_screen = '330'
                    AND u.fiscal_year = @Year
                    AND u.company_code = @CompanyCode
                    AND u.section_code = @SectionCode";


            var param = new { Year = year, CompanyCode = companyCode, SectionCode = sectionCode };
            LogHelper.Debug($"[SQL Execution - GetManagementInputFlagsAsync]\n{sql}\nParameters: {JsonSerializer.Serialize(param)}");

            var result = await conn.QueryFirstOrDefaultAsync<bool?>(sql, param);
            return result ?? false;
        }

        /// <summary>
        /// 指定された部門の月別経費予算データを科目・細目ごとに月展開して集計取得します。
        /// </summary>
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

            var param = new { Year = year, SectionCode = sectionCode };
            LogHelper.Debug($"[SQL Execution - GetBudgetDataForExcelAsync]\n{sql}\nParameters: {JsonSerializer.Serialize(param)}");

            return await conn.QueryAsync<MonthlyBudgetData>(sql, param);
        }


        /// <summary>
        /// 部門別経費予算データのUPSERT（登録・更新）を行います。
        /// （同一キーが存在する場合は金額等の情報を更新します）
        /// </summary>
        public async Task<int> UpsertAsync(IDbConnection conn, dynamic param)
        {
            string sql = @"
                INSERT INTO YT_YOSAN ( 
                  data_type, fiscal_year, mgmt_level, org_code, brand_code, 
                  main_product_dept_code, product_category, account_expense_type, account_code, sub_account_code, 
                  data_category, year_month, fiscal_month, allocation_source_code, budget_amount, 
                  rate, category, flag_1, flag_2, flag_3, 
                  flag_4, flag_5, created_program, updated_program, delete_flag, 
                  created_by, created_at, updated_by, updated_at 
                ) VALUES ( 
                  '10', @FiscalYear, '020', @OrgCode, '99999999', 
                  '99999999', '9', @AccountExpenseType, @AccountCode, @SubAccountCode, 
                  '1', @YearMonth, LPAD(@FiscalMonth::text, 2, '0'), '99999999', @BudgetAmount, 
                  0, '0', '0', '0', '0', 
                  '0', '0', @CreatedProgram, NULL, @DeleteFlag, 
                  @CreatedBy, CURRENT_TIMESTAMP, NULL, NULL 
                )
                ON CONFLICT (
                  data_type, fiscal_year, mgmt_level, org_code, brand_code, 
                  main_product_dept_code, product_category, account_expense_type, account_code, sub_account_code, 
                  data_category, year_month, fiscal_month, allocation_source_code
                )
                DO UPDATE SET 
                  budget_amount = EXCLUDED.budget_amount,
                  delete_flag = EXCLUDED.delete_flag,
                  updated_program = @UpdatedProgram,
                  updated_by = @UpdatedBy,
                  updated_at = CURRENT_TIMESTAMP;";

            LogHelper.Debug($"[SQL Execution - UpsertAsync]\nParameters: {JsonSerializer.Serialize((object)param)}");

            return await conn.ExecuteAsync(sql, (object)param);
        }

        /// <summary>
        /// 運用管理テーブルに対して、担当入力完了フラグを登録・更新（UPSERT）します。
        /// </summary>
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

            var param = new { Year = year, CompanyCode = companyCode, SectionCode = sectionCode, StaffHandlerCode = staffHandlerCode, DeleteFlag = deleteFlag, CreatedBy = createdBy, CreatedAt = createdAt, UpdatedBy = updatedBy, UpdatedAt = updatedAt };
            LogHelper.Debug($"[SQL Execution - UpsertStaffInputStatusAsync]\n{sql}\nParameters: {JsonSerializer.Serialize(param)}");

            await conn.ExecuteAsync(sql, param);
        }

        /// <summary>
        /// 運用管理テーブルの既存レコードに対して、担当入力完了フラグを更新（UPDATE）します。
        /// </summary>
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

            var param = new { StaffHandlerCode = staffHandlerCode, UpdatedBy = updatedBy, UpdatedAt = updatedAt, Year = year, CompanyCode = companyCode, SectionCode = sectionCode };
            LogHelper.Debug($"[SQL Execution - UpdateStaffInputStatusAsync]\n{sql}\nParameters: {JsonSerializer.Serialize(param)}");

            await conn.ExecuteAsync(sql, param);
        }
    }
}