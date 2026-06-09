using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Npgsql;
using Dapper;

namespace BudgetManagement.Repositories
{
    // =========================================================
    // 💡 SQLの結果を受け取るためのデータの「箱（DTO）」クラス群
    // =========================================================
    public class CompanyInfo
    {
        public string CompanyCode { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
    }

    public class SectionInfo
    {
        public string SectionCode { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
    }

    public class ExcelBudgetData
    {
        public string AccountCode { get; set; } = string.Empty;
        public string SubAccountCode { get; set; } = string.Empty;
        public decimal BudgetApr { get; set; }
        public decimal BudgetMay { get; set; }
        public decimal BudgetJun { get; set; }
        public decimal BudgetJul { get; set; }
        public decimal BudgetAug { get; set; }
        public decimal BudgetSep { get; set; }
        public decimal BudgetOct { get; set; }
        public decimal BudgetNov { get; set; }
        public decimal BudgetDec { get; set; }
        public decimal BudgetJan { get; set; }
        public decimal BudgetFeb { get; set; }
        public decimal BudgetMar { get; set; }
    }

    // =========================================================
    // 🛠️ 部門別経費予算 SQL実行リポジトリクラス
    // =========================================================
    public class DepartmentBudgetRepository
    {
        private readonly string _connectionString;

        public DepartmentBudgetRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// データベース接続を生成するヘルパー
        /// </summary>
        private IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);

        // =========================================================
        // ① 年度テキストボックス変更時に会社コードと名称を取得
        // =========================================================
        public List<CompanyInfo> GetCompaniesByYear(string year)
        {
            string sql = @"
                SELECT
                    s.kaisyacd AS CompanyCode,
                    m.ryaku AS CompanyName
                FROM
                    ym_soshiki s
                JOIN
                    m_mei m ON s.kaisyacd = m.meicd
                WHERE
                    s.nendo = @Year
                    AND s.kanrilevel = '070'
                    AND m.meiid = 'SSKIZOKU16'
                ORDER BY
                    s.kaisyacd;";

            using (var db = CreateConnection())
            {
                return db.Query<CompanyInfo>(sql, new { Year = year }).ToList();
            }
        }

        // =========================================================
        // ② 年度テキストボックス変更時に課コードと名称を取得
        // =========================================================
        public List<SectionInfo> GetSections(string year, string companyCode)
        {
            string sql = @"
                SELECT
                    s.kacd AS SectionCode,
                    m.ryaku AS SectionName
                FROM
                    ym_soshiki s
                INNER JOIN 
                    m_mei m ON s.kacd = m.meicd
                WHERE
                    s.nendo = @Year
                    AND s.kaisyacd = @CompanyCode
                    AND s.kanrilevel = '020'
                    AND m.meiid = 'SSKIZOKU3'
                    AND s.bmnbetukeihiysninpkbn = '1'
                ORDER BY
                    s.kacd;";

            using (var db = CreateConnection())
            {
                return db.Query<SectionInfo>(sql, new { Year = year, CompanyCode = companyCode }).ToList();
            }
        }

        // =========================================================
        // ③ 課のコンボボックス確定時に管理入力完了フラグを取得
        // =========================================================
        public List<string> GetManagementInputFlags(string year, string companyCode, string sectionCode)
        {
            string sql = @"
                SELECT
                    u.kanriinpflg
                FROM
                    ym_unyou u
                WHERE
                    u.inputscreen = '330'
                    AND u.nendo = @Year
                    AND u.kaisyacd = @CompanyCode
                    AND u.kacd = @SectionCode
                    AND u.kanriinpflg = '1'
                UNION ALL
                SELECT
                    u.tinpflg AS kanriinpflg
                FROM
                    ym_unyou u
                WHERE
                    u.inputscreen = '360'
                    AND u.nendo = @Year
                    AND u.kaisyacd = @CompanyCode
                    AND u.tinpflg = '1';";

            using (var db = CreateConnection())
            {
                return db.Query<string>(sql, new { Year = year, CompanyCode = companyCode, SectionCode = sectionCode }).ToList();
            }
        }

        // =========================================================
        // ④ Excel出力ボタン押下時に、予算データを取得
        // =========================================================
        public List<ExcelBudgetData> GetBudgetDataForExcel(string year, string companyCode, string sectionCode)
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

            using (var db = CreateConnection())
            {
                return db.Query<ExcelBudgetData>(sql, new { Year = year, CompanyCode = companyCode, SectionCode = sectionCode }).ToList();
            }
        }

        // =========================================================
        // ⑤ Excelを取り込んで、そのデータを予算データへ更新（UPSERT）
        // =========================================================
        public void UpsertYosanData(IDbTransaction transaction, dynamic p)
        {
            string sql = @"
                INSERT INTO YT_YOSAN (
                    data_type, fiscal_year, mgmt_level, org_code, main_product_dept_code,
                    brand_code, product_category, account_expense_type, account_code, sub_account_code,
                    data_category, year_month, fiscal_month, allocation_source_code, budget_amount,
                    rate, category, flag_1, flag_2, flag_3,
                    flag_4, flag_5, created_program, updated_program, delete_flag,
                    created_by, created_at, updated_by, updated_at
                )
                VALUES (
                    '10', @FiscalYear, '020', @OrgCode, '99999999',
                    '99999999', '9', @AccountExpenseType, @AccountCode, @SubAccountCode,
                    '1', @YearMonth, LPAD(@FiscalMonth, 2, '0'), '99999999', @BudgetAmount,
                    0, '0', '0', '0', '0',
                    '0', '0', @CreatedProgram, @UpdatedProgram, @DeleteFlag,
                    @CreatedBy, @CreatedAt, @UpdatedBy, @UpdatedAt
                )
                ON CONFLICT (
                    data_type, fiscal_year, mgmt_level, org_code, main_product_dept_code,
                    brand_code, product_category, account_expense_type, account_code, sub_account_code,
                    data_category, year_month, fiscal_month, allocation_source_code
                ) 
                DO UPDATE SET
                    budget_amount   = EXCLUDED.budget_amount,
                    rate            = EXCLUDED.rate,
                    category        = EXCLUDED.category,
                    flag_1          = EXCLUDED.flag_1,
                    flag_2          = EXCLUDED.flag_2,
                    flag_3          = EXCLUDED.flag_3,
                    flag_4          = EXCLUDED.flag_4,
                    flag_5          = EXCLUDED.flag_5,
                    updated_program = EXCLUDED.updated_program,
                    delete_flag     = EXCLUDED.delete_flag,
                    updated_by      = EXCLUDED.updated_by,
                    updated_at      = EXCLUDED.updated_at;";

            // トランザクション（一括処理）を考慮し、Connectionは外側から受け取る設計にしています
            transaction.Connection!.Execute(sql, (object)p, transaction);
        }

        // =========================================================
        // ⑥ 担当入力完了フラグを登録する（旧カラム形式：日付と時刻が別）
        // =========================================================
        public void UpsertStaffInputStatusLegacy(string year, string companyCode, string sectionCode, string staffHandlerCode, string deleteFlag, string createdBy, string createdYmd, string createdJikan, string updatedBy, string updatedYmd, string updatedJikan)
        {
            string sql = @"
                INSERT INTO ym_unyou (
                    input_screen, fiscal_year, company_code, dept_code, section_code, 
                    staff_code, staff_input_complete_flag, staff_handler_code, delete_flag, 
                    created_by, created_date, updated_by, updated_date
                )
                VALUES (
                    '330', @Year, @CompanyCode, '99999999', @SectionCode,
                    '99999999', true, @StaffHandlerCode, @DeleteFlag::boolean,
                    @CreatedBy, TO_TIMESTAMP(@CreatedTimestamp, 'YYYYMMDDHH24MISS'),
                    @UpdatedBy, TO_TIMESTAMP(@UpdatedTimestamp, 'YYYYMMDDHH24MISS')
                )
                ON CONFLICT (input_screen, fiscal_year, company_code, section_code, staff_code)
                DO UPDATE SET
                    staff_input_complete_flag = EXCLUDED.staff_input_complete_flag,
                    staff_handler_code        = EXCLUDED.staff_handler_code,
                    updated_by                = EXCLUDED.updated_by,
                    updated_date              = EXCLUDED.updated_date;";

            var parameters = new
            {
                Year = year,
                CompanyCode = companyCode,
                SectionCode = sectionCode,
                StaffHandlerCode = staffHandlerCode,
                DeleteFlag = deleteFlag,
                CreatedBy = createdBy,
                CreatedTimestamp = createdYmd + createdJikan,
                UpdatedBy = updatedBy,
                UpdatedTimestamp = updatedYmd + updatedJikan
            };

            using (var db = CreateConnection())
            {
                db.Execute(sql, parameters);
            }
        }

        // =========================================================
        // ⑦ 担当入力完了フラグを登録する（新カラム形式：統合timestamp、target_month有）
        // =========================================================
        public void UpsertStaffInputStatus(string year, string companyCode, string sectionCode, string staffHandlerCode, string deleteFlag, string createdBy, DateTime createdAt, string updatedBy, DateTime updatedAt)
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

            var parameters = new
            {
                Year = year,
                CompanyCode = companyCode,
                SectionCode = sectionCode,
                StaffHandlerCode = staffHandlerCode,
                DeleteFlag = deleteFlag,
                CreatedBy = createdBy,
                CreatedAt = createdAt,
                UpdatedBy = updatedBy,
                UpdatedAt = updatedAt
            };

            using (var db = CreateConnection())
            {
                db.Execute(sql, parameters);
            }
        }

        // =========================================================
        // ⑧ 予算データをupdateした場合、担当入力完了フラグを更新する。
        // =========================================================
        public int UpdateStaffInputStatus(string staffHandlerCode, string updatedBy, DateTime updatedAt, string year, string companyCode, string sectionCode)
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

            var parameters = new
            {
                StaffHandlerCode = staffHandlerCode,
                UpdatedBy = updatedBy,
                UpdatedAt = updatedAt,
                Year = year,
                CompanyCode = companyCode,
                SectionCode = sectionCode
            };

            using (var db = CreateConnection())
            {
                return db.Execute(sql, parameters); // 更新された行数を返します
            }
        }
    }
}