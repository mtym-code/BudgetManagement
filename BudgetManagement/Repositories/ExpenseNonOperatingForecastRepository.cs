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
    public class SystemYearMonth
    {
        public string YearMonth { get; set; } = string.Empty;
        public string Month { get; set; } = string.Empty;
        public string DisplayMonth => $"{Month}月"; // 表示用
    }

    public class ForecastData
    {
        public string DivisionCode { get; set; } = string.Empty;
        public string DivisionName { get; set; } = string.Empty;
        public string DeptCode { get; set; } = string.Empty;
        public string DeptName { get; set; } = string.Empty;

        public decimal? Keihi1 { get; set; }
        public decimal? Keihi2 { get; set; }
        public decimal? Syueki { get; set; }
        public decimal? Keihimitoshi1 { get; set; }
        public decimal? Keihimitoshi2 { get; set; }
        public decimal? Syuekimitoshi { get; set; }
    }
}

namespace BudgetManagement.Repositories
{
    public class ExpenseNonOperatingForecastRepository
    {
        public async Task<IEnumerable<SystemYearMonth>> GetSystemYearMonthsAsync(IDbConnection conn, string year)
        {
            string sql = @"
                SELECT
                    year_month AS YearMonth,
                    SUBSTR(year_month, 5, 2) AS Month
                FROM (
                    SELECT TO_CHAR(CURRENT_DATE - INTERVAL '4 months', 'YYYY') || TO_CHAR(CURRENT_DATE - INTERVAL '1 month', 'MM') AS year_month
                    UNION ALL
                    SELECT TO_CHAR(CURRENT_DATE - INTERVAL '3 months', 'YYYY') || TO_CHAR(CURRENT_DATE, 'MM') AS year_month
                    UNION ALL
                    SELECT TO_CHAR(CURRENT_DATE - INTERVAL '2 months', 'YYYY') || TO_CHAR(CURRENT_DATE + INTERVAL '1 month', 'MM') AS year_month
                ) base_query
                WHERE
                    SUBSTR(year_month, 1, 4) = @Year
                ORDER BY
                    year_month;";
            return await conn.QueryAsync<SystemYearMonth>(sql, new { Year = year });
        }

        public async Task<IEnumerable<Company>> GetCompaniesByYearAsync(IDbConnection conn, string year)
        {
            string sql = @"
                SELECT
                    s.company_code AS CompanyCode,
                    m.abbreviation_name AS CompanyName
                FROM ym_soshiki s
                JOIN m_mei m ON s.company_code = m.name_code
                WHERE s.fiscal_year = @Year AND s.mgmt_level = '070' AND m.name_id = 'SSKIZOKU16'
                ORDER BY s.company_code;";
            return await conn.QueryAsync<Company>(sql, new { Year = year });
        }

        public async Task<bool> GetStaffInputCompleteFlagAsync(IDbConnection conn, string year, string companyCode, int targetMonth)
        {
            string sql = @"
                SELECT u.staff_input_complete_flag FROM ym_unyou u
                WHERE u.input_screen = '360' AND u.fiscal_year = @Year AND u.company_code = @CompanyCode AND u.target_month = @TargetMonth;";
            return await conn.QueryFirstOrDefaultAsync<bool?>(sql, new { Year = year, CompanyCode = companyCode, TargetMonth = targetMonth }) ?? false;
        }

        public async Task<IEnumerable<ForecastData>> GetForecastDataAsync(IDbConnection conn, string year, string companyCode, string targetMonth)
        {
            string sql = @"
                SELECT
                    s.division_code AS DivisionCode,
                    m1.abbreviation_name AS DivisionName,
                    s.dept_code AS DeptCode,
                    m2.abbreviation_name AS DeptName,
                    ck1.keihi1 AS Keihi1,
                    ck2.keihi2 AS Keihi2,
                    cs.syueki AS Syueki,
                    mk1.keihimitoshi1 AS Keihimitoshi1,
                    mk2.keihimitoshi2 AS Keihimitoshi2,
                    ms.syuekimitoshi AS Syuekimitoshi
                FROM
                    ym_soshiki s

                INNER JOIN m_mei m1 ON s.division_code = m1.name_code AND m1.name_id = 'SSKIZOKU18'
                INNER JOIN m_mei m2 ON s.dept_code = m2.name_code AND m2.name_id = 'SSKIZOKU20'

                LEFT OUTER JOIN (
                    SELECT s.dept_code, SUM(y.budget_amount) AS keihi1
                    FROM yt_yosan y
                    INNER JOIN ym_soshiki s ON s.section_code = y.org_code AND s.fiscal_year = @Year AND s.mgmt_level = '020'
                    WHERE y.data_type = '10' AND y.fiscal_year = @Year AND y.mgmt_level = '020'
                      AND y.account_expense_type = '1' AND y.account_code = '711AAA' AND y.fiscal_month = LPAD(@TargetMonth::text, 2, '0')
                    GROUP BY s.dept_code
                ) ck1 ON s.dept_code = ck1.dept_code

                LEFT OUTER JOIN (
                    SELECT s.dept_code, SUM(y.budget_amount) AS keihi2
                    FROM yt_yosan y
                    INNER JOIN ym_soshiki s ON s.section_code = y.org_code AND s.fiscal_year = @Year AND s.mgmt_level = '020'
                    WHERE y.data_type = '10' AND y.fiscal_year = @Year AND y.mgmt_level = '020'
                      AND y.account_expense_type = '1' AND y.account_code = '762AAA' AND y.fiscal_month = LPAD(@TargetMonth::text, 2, '0')
                    GROUP BY s.dept_code
                ) ck2 ON s.dept_code = ck2.dept_code

                LEFT OUTER JOIN (
                    SELECT s.dept_code, SUM(y.budget_amount) AS syueki
                    FROM yt_yosan y
                    INNER JOIN ym_soshiki s ON s.section_code = y.org_code AND s.fiscal_year = @Year AND s.mgmt_level = '020'
                    WHERE y.data_type = '10' AND y.fiscal_year = @Year AND y.mgmt_level = '020'
                      AND y.account_expense_type = '1' AND y.account_code = '811AAA' AND y.fiscal_month = LPAD(@TargetMonth::text, 2, '0')
                    GROUP BY s.dept_code
                ) cs ON s.dept_code = cs.dept_code

                LEFT OUTER JOIN (
                    SELECT org_code, SUM(budget_amount) AS keihimitoshi1
                    FROM yt_yosan
                    WHERE data_type = '20' AND fiscal_year = @Year AND mgmt_level = '030'
                      AND account_expense_type = '0' AND account_code = '711000' AND fiscal_month = LPAD(@TargetMonth::text, 2, '0')
                    GROUP BY org_code
                ) mk1 ON s.dept_code = mk1.org_code

                LEFT OUTER JOIN (
                    SELECT org_code, SUM(budget_amount) AS keihimitoshi2
                    FROM yt_yosan
                    WHERE data_type = '20' AND fiscal_year = @Year AND mgmt_level = '030'
                      AND account_expense_type = '0' AND account_code = '762000' AND fiscal_month = LPAD(@TargetMonth::text, 2, '0')
                    GROUP BY org_code
                ) mk2 ON s.dept_code = mk2.org_code

                LEFT OUTER JOIN (
                    SELECT org_code, SUM(budget_amount) AS syuekimitoshi
                    FROM yt_yosan
                    WHERE data_type = '20' AND fiscal_year = @Year AND mgmt_level = '030'
                      AND account_expense_type = '0' AND account_code = '811000' AND fiscal_month = LPAD(@TargetMonth::text, 2, '0')
                    GROUP BY org_code
                ) ms ON s.dept_code = ms.org_code

                WHERE
                    s.fiscal_year               = @Year
                    AND s.mgmt_level            = '030'
                    AND s.company_code          = @CompanyCode
                    AND s.expense_non_operating_forecast_type = '1'
                ORDER BY
                    s.division_code,
                    s.dept_code;";

            return await conn.QueryAsync<ForecastData>(sql, new { Year = year, CompanyCode = companyCode, TargetMonth = targetMonth });
        }

        public async Task<int> UpsertForecastDataAsync(IDbConnection conn, string year, string orgCode, string accountCode, string yearMonth, string fiscalMonth, decimal budgetAmount, string programId, bool deleteFlag, string userId)
        {
            string sql = @"
                INSERT INTO yt_yosan (
                    data_type, fiscal_year, mgmt_level, org_code,
                    main_product_dept_code, brand_code, product_category,
                    account_expense_type, account_code, sub_account_code, data_category,
                    year_month, fiscal_month, allocation_source_code,
                    budget_amount, rate, category,
                    flag_1, flag_2, flag_3, flag_4, flag_5,
                    created_program, updated_program, delete_flag,
                    created_by, created_at, updated_by, updated_at
                ) VALUES (
                    '20', @Year, '030', @OrgCode,
                    '99999999', '99999999', '9',
                    '0', @AccountCode, '00', '1',
                    @YearMonth, LPAD(@FiscalMonth::text, 2, '0'), '99999999',
                    @BudgetAmount, 0, '0',
                    '0', '0', '0', '0', '0',
                    @ProgramId, NULL, @DeleteFlag,
                    @UserId, CURRENT_TIMESTAMP, NULL, NULL
                )
                ON CONFLICT (
                    data_type, fiscal_year, mgmt_level, org_code, brand_code, 
                    main_product_dept_code, product_category, account_expense_type, account_code, sub_account_code, 
                    data_category, year_month, fiscal_month, allocation_source_code
                )
                DO UPDATE SET
                    budget_amount = EXCLUDED.budget_amount,
                    delete_flag = EXCLUDED.delete_flag,
                    updated_program = @ProgramId,
                    updated_by = @UserId,
                    updated_at = CURRENT_TIMESTAMP;";

            return await conn.ExecuteAsync(sql, new { Year = year, OrgCode = orgCode, AccountCode = accountCode, YearMonth = yearMonth, FiscalMonth = fiscalMonth, BudgetAmount = budgetAmount, ProgramId = programId, DeleteFlag = deleteFlag, UserId = userId });
        }

        public async Task UpsertStaffInputStatusAsync(IDbConnection conn, string year, string companyCode, int targetMonth, string staffHandlerCode, bool deleteFlag, string userId)
        {
            string sql = @"
                INSERT INTO ym_unyou (
                    input_screen, fiscal_year, company_code, dept_code, section_code, staff_code, target_month,
                    staff_input_complete_flag, staff_handler_code, delete_flag, created_by, created_at, updated_by, updated_at
                ) VALUES (
                    '360', @Year, @CompanyCode, '99999999', '99999999', '99999999', @TargetMonth,
                    true, @StaffHandlerCode, @DeleteFlag, @UserId, CURRENT_TIMESTAMP, @UserId, CURRENT_TIMESTAMP
                )
                /* 🌟 修正ポイント：主キー項目をすべて指定しました */
                ON CONFLICT (input_screen, fiscal_year, company_code, dept_code, section_code, staff_code, target_month)
                DO UPDATE SET
                    staff_input_complete_flag = true,
                    staff_handler_code        = EXCLUDED.staff_handler_code,
                    updated_by                = EXCLUDED.updated_by,
                    updated_at                = CURRENT_TIMESTAMP;";
            
            await conn.ExecuteAsync(sql, new { Year = year, CompanyCode = companyCode, TargetMonth = targetMonth, StaffHandlerCode = staffHandlerCode, DeleteFlag = deleteFlag, UserId = userId });
        }
    }
}