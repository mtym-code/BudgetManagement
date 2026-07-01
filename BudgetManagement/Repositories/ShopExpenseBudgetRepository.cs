using BudgetManagement.Common.Helper;
using BudgetManagement.Models;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;

namespace BudgetManagement.Repositories
{
    // =========================================================
    // 🛠️ ショップ経費予算 SQL実行リポジトリクラス
    // =========================================================
    public class ShopExpenseBudgetRepository
    {
        // ---------------------------------------------------------
        // 動作確認用：DapperパラメータをSQL文字列に埋め込んだ確認用SQLを作る
        // ※ 実際にDBへ渡すSQLは @Year などのバインド変数付きSQLです。
        // ※ ログ確認・DBクライアント貼り付け確認用です。
        // ---------------------------------------------------------
        private static string BuildDebugSql(string sql, object param)
        {
            static string ToSqlLiteral(object? value)
            {
                if (value == null)
                {
                    return "NULL";
                }

                if (value is string s)
                {
                    return $"'{s.Replace("'", "''")}'";
                }

                if (value is DateTime dt)
                {
                    return $"'{dt:yyyy-MM-dd HH:mm:ss}'";
                }

                if (value is bool b)
                {
                    return b ? "true" : "false";
                }

                return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "NULL";
            }

            string debugSql = sql;

            foreach (var property in param.GetType().GetProperties())
            {
                string key = "@" + property.Name;
                string value = ToSqlLiteral(property.GetValue(param));

                debugSql = debugSql.Replace(key, value);
            }

            return debugSql;
        }

        // ---------------------------------------------------------
        // FY350_01: 会社データ取得
        // ---------------------------------------------------------
        public async Task<IEnumerable<dynamic>> GetCompaniesAsync(IDbConnection conn, string year)
        {
            string sql = @"
                SELECT 
                    s.company_code, 
                    m.abbreviation_name AS ryaku 
                FROM 
                    ym_soshiki s
                INNER JOIN 
                    m_mei m ON s.company_code = m.name_code 
                WHERE 
                    s.fiscal_year = @Year 
                    AND s.mgmt_level = '070' 
                    AND m.name_id = 'SSKIZOKU16' 
                ORDER BY 
                    s.company_code;";

            var param = new { Year = year };
            LogHelper.Debug($"[SQL - GetCompaniesAsync]\n{sql}\nParam: {JsonSerializer.Serialize(param)}");
            return await conn.QueryAsync(sql, param);
        }

        // ---------------------------------------------------------
        // FY350_02: 得意先（ショップ）データ取得
        // ---------------------------------------------------------
        public async Task<IEnumerable<OperationMeiItem>> GetCustomersAsync(IDbConnection conn, string year, string companyCode)
        {
            string sql = @"
                SELECT 
                    s.customer_code AS NameCode,
                    m.abbreviation_name AS AbbreviationName
                FROM 
                    ym_soshiki s
                INNER JOIN 
                    m_mei m ON s.customer_code = m.name_code 
                WHERE 
                    s.fiscal_year = @Year 
                    AND s.company_code = @CompanyCode 
                    AND s.mgmt_level = '010' 
                    AND m.name_id = 'SSKIZOKU4' 
                GROUP BY 
                    s.customer_code, m.abbreviation_name 
                ORDER BY 
                    s.customer_code;";

            var param = new { Year = year, CompanyCode = companyCode };
            LogHelper.Debug($"[SQL - GetCustomersAsync]\n{sql}\nParam: {JsonSerializer.Serialize(param)}");

            return await conn.QueryAsync<OperationMeiItem>(sql, param);
        }

        // ---------------------------------------------------------
        // FY350_03: 排他制御チェック
        // ---------------------------------------------------------
        public async Task<string> CheckLockAsync(IDbConnection conn, string year, string orgCode)
        {
            string sql = @"
                SELECT 
                    tano 
                FROM 
                    t_haitaseigyo 
                WHERE 
                    fiscal_year = @Year 
                    AND mgmt_level = '010' 
                    AND org_code = @OrgCode 
                    AND input_screen = '350';";

            var param = new { Year = year, OrgCode = orgCode };
            LogHelper.Debug($"[SQL - CheckLockAsync]\n{sql}");
            return await conn.QueryFirstOrDefaultAsync<string>(sql, param);
        }

        // ---------------------------------------------------------
        // FY350_04: 排他制御ロック登録
        // ---------------------------------------------------------
        public async Task<int> InsertLockAsync(IDbConnection conn, dynamic param)
        {
            string sql = @"
                INSERT INTO t_haitaseigyo ( 
                    org_code, tano, hmban, sub1, sub2, sub3, input_screen, 
                    fiscal_year, mgmt_level, soshikicd, staff_code, account_code, 
                    delete_flag, created_by, created_at, updated_by, updated_at 
                ) VALUES ( 
                    '00000000', @TaNo, '9999999999', '99999999', '99999999', '99999999', '350', 
                    @Year, '010', @SoshikiCd, @StaffCode, '999999', 
                    @DeleteFlag, @CreatedBy, CURRENT_TIMESTAMP, @UpdatedBy, CURRENT_TIMESTAMP 
                );";

            LogHelper.Debug($"[SQL - InsertLockAsync]\nParam: {JsonSerializer.Serialize((object)param)}");
            return await conn.ExecuteAsync(sql, (object)param);
        }

        // ---------------------------------------------------------
        // FY350_05: ショップ経費予算データ取得（月次展開版）
        // SH0210 バックス変動はこのSQL内で算出する
        // ---------------------------------------------------------
        public async Task<IEnumerable<MonthlyBudgetData>> GetMonthlyBudgetDataAsync(
            IDbConnection conn,
            string year,
            string companyCode,
            string customerCode)
        {
            string sql = @"
SELECT
                      Y.account_code || '-' || Y.org_code AS datakey
                    , Y.account_code
                    , Y.fiscal_month
                    , ROUND(Y.budget_amount, 0) AS budget_amount
                    , CASE 
                        WHEN Y.fiscal_month::INTEGER < 4 THEN Y.fiscal_month::INTEGER + 9 
                        ELSE Y.fiscal_month::INTEGER - 3 
                      END AS col 
                FROM (
                      SELECT
                            Y.account_code
                          , Y.org_code
                          , Y.fiscal_month
                          , SUM(Y.budget_amount) AS budget_amount 
                      FROM  ym_soshiki S
                          , yt_yosan   Y 
                      WHERE S.fiscal_year    = @Year 
                        AND S.mgmt_level     = '010' 
                        AND S.company_code   = @CompanyCode 
                        AND S.customer_code  = @CustomerCode 
                        AND Y.data_type      = '10' 
                        AND Y.fiscal_year    = S.fiscal_year 
                        AND Y.org_code       = S.customer2_code 
                        AND Y.mgmt_level     = '010' 
                        AND Y.data_category  = '1' 
                        AND Y.account_expense_type = '1' 
                        AND Y.account_code NOT IN ( 'SH0010', 'SH0020', 'SH0030', 'SH0040', 'SH0050', 'SH0060', 'SH0210', 'SH0220' ) 
                      GROUP BY Y.account_code, Y.org_code, Y.fiscal_month 
                
                      UNION ALL 
                
                      SELECT
                            CASE 
                              WHEN Y.account_code = 'TK0010' THEN 'SH0010' 
                              WHEN Y.account_code = 'TK0030' THEN 'SH0020' 
                              WHEN Y.account_code = 'TK0050' THEN 'SH0030' 
                              WHEN Y.account_code = 'TK0060' THEN 'SH0040' 
                            END AS account_code
                          , Y.org_code
                          , Y.fiscal_month
                          , SUM(Y.budget_amount) AS budget_amount 
                      FROM  ym_soshiki S
                          , yt_yosan   Y 
                      WHERE S.fiscal_year    = @Year 
                        AND S.mgmt_level     = '010' 
                        AND S.company_code   = @CompanyCode 
                        AND S.customer_code  = @CustomerCode 
                        AND Y.data_type      = '10' 
                        AND Y.fiscal_year    = S.fiscal_year 
                        AND Y.org_code       = S.customer2_code 
                        AND Y.mgmt_level     = '010' 
                        AND Y.data_category  = '1' 
                        AND Y.account_expense_type = '1' 
                        AND Y.account_code IN ('TK0010', 'TK0030', 'TK0050', 'TK0060') 
                      GROUP BY Y.account_code, Y.org_code, Y.fiscal_month 
                
                      UNION ALL 
                
                      SELECT
                            'SH0050' AS account_code
                          , Y.org_code
                          , Y.fiscal_month
                          , SUM( COALESCE(Y.budget_amount, 0) * ( (S2.planning_r_rate_3 / 100) - (S2.product_cost_rate_3 / 100) ) * - 1) AS budget_amount 
                      FROM  ym_soshiki S
                          , ym_soshiki S2
                          , yt_yosan   Y 
                      WHERE S.fiscal_year    = @Year 
                        AND S.mgmt_level     = '010' 
                        AND S.company_code   = @CompanyCode 
                        AND S.customer_code  = @CustomerCode 
                        AND Y.data_type      = '10' 
                        AND Y.fiscal_year    = S.fiscal_year 
                        AND Y.org_code       = S.customer2_code 
                        AND Y.mgmt_level     = '010' 
                        AND Y.data_category  = '1' 
                        AND Y.account_expense_type = '1' 
                        AND Y.account_code   = 'TK0010' 
                        AND S2.fiscal_year   = @Year 
                        AND S2.mgmt_level    = '030' 
                        AND S2.company_code  = @CompanyCode 
                        AND S2.dept_code     = S.dept_code 
                      GROUP BY Y.org_code, Y.fiscal_month 
                
                      UNION ALL 
                
                      SELECT
                            'SH0060' AS account_code
                          , Y.org_code
                          , Y.fiscal_month
                          , SUM( COALESCE(Y.budget_amount, 0) * (S2.store_eval_loss_rate_3 / 100) * - 1) AS budget_amount 
                      FROM  ym_soshiki S
                          , ym_soshiki S2
                          , yt_yosan   Y 
                      WHERE S.fiscal_year    = @Year 
                        AND S.mgmt_level     = '010' 
                        AND S.company_code   = @CompanyCode 
                        AND S.customer_code  = @CustomerCode 
                        AND Y.data_type      = '10' 
                        AND Y.fiscal_year    = S.fiscal_year 
                        AND Y.org_code       = S.customer2_code 
                        AND Y.mgmt_level     = '010' 
                        AND Y.data_category  = '1' 
                        AND Y.account_expense_type = '1' 
                        AND Y.account_code   = 'TK0050' 
                        AND S2.fiscal_year   = @Year 
                        AND S2.mgmt_level    = '030' 
                        AND S2.company_code  = @CompanyCode 
                        AND S2.dept_code     = S.dept_code 
                      GROUP BY Y.org_code, Y.fiscal_month 
                
                      UNION ALL 
                
                      SELECT
                            B.account_code
                          , B.org_code
                          , B.fiscal_month
                          , SUM(B.budget_amount) AS budget_amount 
                      FROM (
                            SELECT
                                  'SH0210' AS account_code
                                , Y.org_code
                                , Y.fiscal_month
                                , SUM(COALESCE(Y.budget_amount, 0) * (K.backs_expense_variable_rate / 100)) AS budget_amount 
                            FROM  ym_soshiki    S
                                , ym_sosikikesu K
                                , yt_yosan      Y 
                            WHERE S.fiscal_year    = @Year 
                              AND S.mgmt_level     = '010' 
                              AND S.company_code   = @CompanyCode 
                              AND S.customer_code  = @CustomerCode 
                              AND Y.data_type      = '10' 
                              AND Y.fiscal_year    = S.fiscal_year 
                              AND Y.org_code       = S.customer2_code 
                              AND Y.mgmt_level     = '010' 
                              AND Y.data_category  = '1' 
                              AND Y.account_expense_type = '1' 
                              AND Y.account_code   = 'TK0030' 
                              AND K.data_category::text = '3' 
                              AND K.fiscal_year    = @Year 
                              AND K.company_code   = @CompanyCode 
                              AND K.dept_code      = S.dept_code 
                              AND K.king_partner_type = S.king_partner_type 
                            GROUP BY Y.org_code, Y.fiscal_month
                
                            UNION ALL
                
                            SELECT
                                  'SH0210' AS account_code
                                , Y.org_code
                                , Y.fiscal_month
                                , COALESCE(Y.budget_amount, 0) * (K.backs_expense_variable_rate / 100) AS budget_amount 
                            FROM  ym_soshiki    S
                                , ym_sosikikesu K
                                , yt_yosan      Y
                                , (
                                    SELECT
                                          division_code
                                        , dept_code
                                        , main_product_dept_code
                                        , brand_code
                                    FROM  ym_jigyobushohinka
                                    GROUP BY division_code
                                           , dept_code
                                           , main_product_dept_code
                                           , brand_code
                                  ) J 
                            WHERE S.fiscal_year           = @Year 
                              AND S.mgmt_level            = '010' 
                              AND S.company_code          = @CompanyCode 
                              AND S.customer_code         = @CustomerCode 
                              AND Y.data_type             = '10' 
                              AND Y.fiscal_year           = S.fiscal_year 
                              AND Y.org_code              = S.customer2_code 
                              AND Y.mgmt_level            = '010' 
                              AND Y.data_category         = '1' 
                              AND Y.account_expense_type  = '1' 
                              AND Y.account_code          = 'TK0030' 
                              AND J.division_code         = S.division_code 
                              AND J.dept_code             = S.dept_code 
                              AND J.brand_code            = Y.brand_code 
                              AND K.data_category::text   = '6' 
                              AND K.fiscal_year           = @Year 
                              AND K.company_code          = @CompanyCode 
                              AND K.dept_code             = S.dept_code 
                              AND K.king_partner_type     = J.main_product_dept_code
                      ) B 
                      GROUP BY B.account_code, B.org_code, B.fiscal_month 
                ) Y
                ORDER BY Y.account_code, col;";

            var param = new { Year = year, CompanyCode = companyCode, CustomerCode = customerCode };
            var debugSql = BuildDebugSql(sql, param);

            try
            {
                LogHelper.Debug(
                    $"[SQL - GetMonthlyBudgetDataAsync / SH0210確認]\n" +
                    $"{debugSql}\n" +
                    $"Param: {JsonSerializer.Serialize(param)}");

                // Dapperで動的に行データを取得
                var rawData = await conn.QueryAsync<dynamic>(sql, param);

                // 取得した縦持ちの月データを、ViewModelが扱いやすい横持ち(Month04〜Month03)にピボット変換
                var resultDict = new Dictionary<string, MonthlyBudgetData>();

                foreach (var row in rawData)
                {
                    string acct = row.account_code;
                    if (!resultDict.ContainsKey(acct))
                    {
                        resultDict[acct] = new MonthlyBudgetData { AccountCode = acct };
                    }

                    int col = Convert.ToInt32(row.col);
                    decimal amt = row.budget_amount != null ? Convert.ToDecimal(row.budget_amount) : 0m;

                    switch (col)
                    {
                        case 1: resultDict[acct].Month04 += amt; break;
                        case 2: resultDict[acct].Month05 += amt; break;
                        case 3: resultDict[acct].Month06 += amt; break;
                        case 4: resultDict[acct].Month07 += amt; break;
                        case 5: resultDict[acct].Month08 += amt; break;
                        case 6: resultDict[acct].Month09 += amt; break;
                        case 7: resultDict[acct].Month10 += amt; break;
                        case 8: resultDict[acct].Month11 += amt; break;
                        case 9: resultDict[acct].Month12 += amt; break;
                        case 10: resultDict[acct].Month01 += amt; break;
                        case 11: resultDict[acct].Month02 += amt; break;
                        case 12: resultDict[acct].Month03 += amt; break;
                    }
                }

                return resultDict.Values;
            }
            catch (Exception ex)
            {
                LogHelper.Debug(
                    $"[ERROR - GetMonthlyBudgetDataAsync / SH0210確認]\n" +
                    $"SQL:\n{debugSql}\n" +
                    $"Param:\n{JsonSerializer.Serialize(param)}\n" +
                    $"Exception:\n{ex}");

                throw;
            }
        }

        // ---------------------------------------------------------
        // FY350_13: バックス固定費取得
        // 現行画面の月別表示に合わせ、SH0220を月別に算出する
        // ---------------------------------------------------------
        public async Task<BudgetManagement.Models.MonthlyBudgetData> GetBacksFixedBudgetDataAsync(
            IDbConnection conn,
            string year,
            string companyCode,
            string customerCode)
        {
            string sql = @"
WITH month_list AS (
    SELECT 4 AS fiscal_month UNION ALL
    SELECT 5 UNION ALL
    SELECT 6 UNION ALL
    SELECT 7 UNION ALL
    SELECT 8 UNION ALL
    SELECT 9 UNION ALL
    SELECT 10 UNION ALL
    SELECT 11 UNION ALL
    SELECT 12 UNION ALL
    SELECT 1 UNION ALL
    SELECT 2 UNION ALL
    SELECT 3
),
target_s AS (
    SELECT
        fiscal_year,
        company_code,
        customer_code,
        customer2_code,
        division_code,
        dept_code,
        king_partner_type,
        tsubo_area
    FROM
        ym_soshiki
    WHERE
        fiscal_year = @Year
        AND mgmt_level = '010'
        AND company_code = @CompanyCode
        AND customer_code = @CustomerCode
),
j AS (
    SELECT
        division_code,
        dept_code,
        main_product_dept_code,
        brand_code
    FROM
        ym_jigyobushohinka
    GROUP BY
        division_code,
        dept_code,
        main_product_dept_code,
        brand_code
),
y_brand_month AS (
    SELECT
        org_code,
        brand_code,
        fiscal_month::integer AS fiscal_month
    FROM
        yt_yosan
    WHERE
        data_type = '10'
        AND fiscal_year = @Year
        AND mgmt_level = '010'
        AND data_category = '1'
        AND account_expense_type = '1'
        AND account_code = 'TK0030'
    GROUP BY
        org_code,
        brand_code,
        fiscal_month::integer
    HAVING
        SUM(COALESCE(budget_amount, 0)) <> 0
)
SELECT
    B.fiscal_month,
    COALESCE(SUM(B.kingaku), 0) AS kingaku
FROM (
    -- data_category = 3 側
    -- 坪単価分は月別ブランドではなく、対象があれば全月に同額展開
    SELECT
        M.fiscal_month,
        SUM(
            ROUND(COALESCE(K.backs_fixed_tsubo_unit_price, 0), -3) / 1000
            * COALESCE(S.tsubo_area, 0)
        ) AS kingaku
    FROM
        target_s S
        CROSS JOIN month_list M
        INNER JOIN ym_sosikikesu K
            ON K.fiscal_year = S.fiscal_year
            AND K.company_code = S.company_code
            AND K.dept_code = S.dept_code
            AND K.king_partner_type = S.king_partner_type
            AND K.data_category::text = '3'
    GROUP BY
        M.fiscal_month

    UNION ALL

    -- data_category = 6 側
    -- 月ごとにTK0030の予算があるブランドだけ、固定経費額を加算する
    SELECT
        Y.fiscal_month,
        SUM(
            ROUND(COALESCE(K.backs_fixed_expense_amount, 0) / 1000, 0)
        ) AS kingaku
    FROM
        target_s S
        INNER JOIN j J
            ON J.division_code = S.division_code
            AND J.dept_code = S.dept_code
        INNER JOIN y_brand_month Y
            ON Y.org_code = S.customer2_code
            AND Y.brand_code = J.brand_code
        INNER JOIN ym_sosikikesu K
            ON K.fiscal_year = S.fiscal_year
            AND K.company_code = S.company_code
            AND K.dept_code = S.dept_code
            AND K.king_partner_type = J.main_product_dept_code
            AND K.data_category::text = '6'
    GROUP BY
        Y.fiscal_month
) B
GROUP BY
    B.fiscal_month
ORDER BY
    CASE
        WHEN B.fiscal_month >= 4 THEN B.fiscal_month
        ELSE B.fiscal_month + 12
    END;";

            var param = new
            {
                Year = year,
                CompanyCode = companyCode,
                CustomerCode = customerCode
            };

            var debugSql = BuildDebugSql(sql, param);

            try
            {
                LogHelper.Debug(
                    $"[SQL - GetBacksFixedBudgetDataAsync / SH0220月別確認]\n" +
                    $"{debugSql}\n" +
                    $"Param: {JsonSerializer.Serialize(param)}");

                var rows = await conn.QueryAsync<dynamic>(sql, param);

                var result = new BudgetManagement.Models.MonthlyBudgetData
                {
                    AccountCode = "SH0220"
                };

                foreach (var row in rows)
                {
                    int fiscalMonth = Convert.ToInt32(row.fiscal_month);
                    decimal amount = row.kingaku != null ? Convert.ToDecimal(row.kingaku) : 0m;

                    switch (fiscalMonth)
                    {
                        case 4: result.Month04 = amount; break;
                        case 5: result.Month05 = amount; break;
                        case 6: result.Month06 = amount; break;
                        case 7: result.Month07 = amount; break;
                        case 8: result.Month08 = amount; break;
                        case 9: result.Month09 = amount; break;
                        case 10: result.Month10 = amount; break;
                        case 11: result.Month11 = amount; break;
                        case 12: result.Month12 = amount; break;
                        case 1: result.Month01 = amount; break;
                        case 2: result.Month02 = amount; break;
                        case 3: result.Month03 = amount; break;
                    }
                }

                LogHelper.Debug(
                    $"[RESULT - GetBacksFixedBudgetDataAsync / SH0220月別確認] " +
                    $"Year={year}, CompanyCode={companyCode}, CustomerCode={customerCode}, " +
                    $"04={result.Month04}, 05={result.Month05}, 06={result.Month06}, " +
                    $"07={result.Month07}, 08={result.Month08}, 09={result.Month09}, " +
                    $"10={result.Month10}, 11={result.Month11}, 12={result.Month12}, " +
                    $"01={result.Month01}, 02={result.Month02}, 03={result.Month03}");

                return result;
            }
            catch (Exception ex)
            {
                LogHelper.Debug(
                    $"[ERROR - GetBacksFixedBudgetDataAsync / SH0220月別確認]\n" +
                    $"SQL:\n{debugSql}\n" +
                    $"Param:\n{JsonSerializer.Serialize(param)}\n" +
                    $"Exception:\n{ex}");

                throw;
            }
        }

        // ---------------------------------------------------------
        // FY350_06: ショップ経費率取得（100%按分ロジック組み込み）
        // ---------------------------------------------------------
        public async Task<IEnumerable<dynamic>> GetShopExpenseRatesAsync(IDbConnection conn, string year, string companyCode, string customerCode)
        {
            string sql = @"
                WITH RawRates AS (
                    SELECT 
                        s.division_code, 
                        m.abbreviation_name AS ryaku, 
                        COALESCE(k.expense_rate, 0) AS expense_rate,
                        ROW_NUMBER() OVER(ORDER BY s.division_code) as rn
                    FROM 
                        ym_soshiki s
                    INNER JOIN 
                        m_mei m ON s.division_code = m.name_code AND m.name_id = 'SSKIZOKU18'
                    LEFT JOIN 
                        ym_shopkeihiritu k ON k.fiscal_year = @Year AND k.customer2_code = s.customer2_code 
                    WHERE 
                        s.fiscal_year = @Year 
                        AND s.mgmt_level = '010' 
                        AND s.company_code = @CompanyCode 
                        AND s.customer_code = @CustomerCode
                ),
                TotalRates AS (
                    SELECT COALESCE(SUM(expense_rate), 0) as other_total
                    FROM RawRates
                    WHERE rn > 1
                )
                SELECT 
                    r.division_code,
                    r.ryaku,
                    CASE 
                        WHEN r.rn = 1 THEN 100.0 - t.other_total
                        ELSE r.expense_rate
                    END AS expense_rate
                FROM 
                    RawRates r
                CROSS JOIN 
                    TotalRates t
                ORDER BY 
                    r.division_code;";

            var param = new { Year = year, CompanyCode = companyCode, CustomerCode = customerCode };

            LogHelper.Debug($"[SQL - GetShopExpenseRatesAsync]\n{sql}");
            return await conn.QueryAsync(sql, param);
        }

        // ---------------------------------------------------------
        // FY350_07: 組織マスタデータ取得（DECODEをCASEに、(+)をLEFT JOINに変更）
        // ---------------------------------------------------------
        public async Task<dynamic> GetOrgMasterDataAsync(IDbConnection conn, string year, string companyCode, string customerCode)
        {
            string sql = @"
                SELECT 
                    TO_CHAR(s_agg.open_date, 'YYYY/MM/DD') AS open_date, 
                    b.product_cost_rate, 
                    b.planning_r_rate, 
                    s_agg.deposit_amount, 
                    s_agg.interior_cost_amount, 
                    s_agg.sales_staff_count, 
                    s_agg.tsubo_area, 
                    CASE WHEN s_agg.tsubo_area != 0 THEN (j.yosangak / 12 / s_agg.tsubo_area) ELSE 0 END AS tsubokoritsu, 
                    s_agg.ryaku 
                FROM 
                (
                    SELECT 
                        SUM(s3.product_cost_rate_3 * (k.keihiritu / 100)) AS product_cost_rate, 
                        SUM(s3.planning_r_rate_3 * (k.keihiritu / 100)) AS planning_r_rate 
                    FROM 
                        ym_soshiki s1
                    INNER JOIN ym_soshiki s3 
                        ON s3.fiscal_year = s1.fiscal_year 
                        AND s3.mgmt_level = '030' 
                        AND s3.company_code = s1.company_code 
                        AND s3.dept_code = s1.dept_code
                    INNER JOIN (
                        SELECT 
                            sk.fiscal_year, sk.division_code, sk.org_code, 
                            CASE 
                                WHEN sk_total.keihiritu != 100 AND sk.division_code = MIN(sk.division_code) OVER (PARTITION BY sk.customer_code) THEN 100 - sk_total.keihiritu 
                                ELSE sk.keihiritu 
                            END AS keihiritu 
                        FROM (
                            SELECT 
                                s.fiscal_year, s.division_code, s.customer_code, s.customer2_code AS org_code, 
                                COALESCE(k.keihiritu, 0) AS keihiritu 
                            FROM ym_soshiki s 
                            LEFT JOIN ym_shopkeihiritu k 
                                ON k.fiscal_year = @Year AND k.org_code = s.customer2_code
                            WHERE s.fiscal_year = @Year AND s.mgmt_level = '010' AND s.company_code = @CompanyCode AND s.customer_code = @CustomerCode
                        ) sk
                        INNER JOIN (
                            SELECT 
                                s.customer_code, SUM(COALESCE(k.keihiritu, 0)) AS keihiritu 
                            FROM ym_soshiki s 
                            LEFT JOIN ym_shopkeihiritu k 
                                ON k.fiscal_year = @Year AND k.org_code = s.customer2_code
                            WHERE s.fiscal_year = @Year AND s.mgmt_level = '010' AND s.company_code = @CompanyCode AND s.customer_code = @CustomerCode 
                            GROUP BY s.customer_code
                        ) sk_total ON sk.customer_code = sk_total.customer_code
                    ) k ON k.fiscal_year = s1.fiscal_year AND k.division_code = s1.division_code AND k.org_code = s1.customer2_code
                    WHERE s1.fiscal_year = @Year AND s1.mgmt_level = '010' AND s1.company_code = @CompanyCode AND s1.customer_code = @CustomerCode
                ) b, 
                (
                    SELECT 
                        MIN(s.open_date) AS open_date, 
                        SUM(ROUND(COALESCE(s.deposit_amount, 0), -3) / 1000) AS deposit_amount, 
                        SUM(ROUND(COALESCE(s.interior_cost_amount, 0), -3) / 1000) AS interior_cost_amount, 
                        SUM(s.sales_staff_count) AS sales_staff_count, 
                        SUM(s.tsubo_area) AS tsubo_area, 
                        MAX(m.abbreviation_name) AS ryaku 
                    FROM ym_soshiki s
                    INNER JOIN m_mei m ON m.name_code = s.trade_type_code AND m.name_id = 'SSKIZOKU8'
                    WHERE s.fiscal_year = @Year AND s.mgmt_level = '010' AND s.company_code = @CompanyCode AND s.customer_code = @CustomerCode
                ) s_agg, 
                (
                    SELECT COALESCE(SUM(y.budget_amount), 0) AS yosangak 
                    FROM ym_soshiki s
                    INNER JOIN yt_yosan y ON y.fiscal_year = s.fiscal_year AND y.org_code = s.customer2_code
                    WHERE s.fiscal_year = @Year AND s.mgmt_level = '010' AND s.company_code = @CompanyCode AND s.customer_code = @CustomerCode
                      AND y.data_type = '10' AND y.mgmt_level = '010' AND y.data_category = '1' AND y.account_expense_type = '1' AND y.account_code = 'TK0050'
                ) j;";

            var param = new { Year = year, CompanyCode = companyCode, CustomerCode = customerCode };
            LogHelper.Debug($"[SQL - GetOrgMasterDataAsync]\n{sql}");
            return await conn.QueryFirstOrDefaultAsync(sql, param);
        }

        // ---------------------------------------------------------
        // FY350_09: 科目情報取得
        // ---------------------------------------------------------
        public async Task<IEnumerable<string>> GetAccountCodesAsync(IDbConnection conn)
        {
            string sql = "SELECT account_code FROM ym_kamoku GROUP BY account_code ORDER BY account_code;";
            return await conn.QueryAsync<string>(sql);
        }

        // ---------------------------------------------------------
        // FY350_10: 予算データの物理削除
        // ---------------------------------------------------------
        public async Task<int> DeleteBudgetAsync(IDbConnection conn, string year, string orgCode, string accountCode)
        {
            string sql = @"
                DELETE FROM yt_yosan 
                WHERE 
                    data_type = '10' 
                    AND fiscal_year = @Year 
                    AND mgmt_level = '010' 
                    AND org_code = @OrgCode 
                    AND account_expense_type = '1' 
                    AND account_code = @AccountCode 
                    AND data_category = '1';";

            var param = new { Year = year, OrgCode = orgCode, AccountCode = accountCode };
            LogHelper.Debug($"[SQL - DeleteBudgetAsync]\n{sql}");
            return await conn.ExecuteAsync(sql, param);
        }

        // ---------------------------------------------------------
        // FY350_11: 予算データの新規登録
        // ---------------------------------------------------------
        public async Task<int> InsertBudgetAsync(IDbConnection conn, dynamic param)
        {
            string sql = @"
                INSERT INTO yt_yosan ( 
                    data_type, fiscal_year, mgmt_level, org_code, main_product_dept_code, 
                    brand_code, product_category, account_expense_type, account_code, sub_account_code, 
                    data_category, year_month, fiscal_month, allocation_source_code, budget_amount, 
                    rate, category, flag_1, flag_2, flag_3, flag_4, flag_5, 
                    created_program, updated_program, delete_flag, created_by, created_at, updated_by, updated_at 
                ) VALUES ( 
                    '10', @FiscalYear, '010', @OrgCode, '99', 
                    '99999999', '9', '1', @AccountCode, '00', 
                    '1', @YearMonth, LPAD(@FiscalMonth::text, 2, '0'), '99999999', @BudgetAmount, 
                    0, '0', '0', '0', '0', '0', '0', 
                    @CreatedProgram, @UpdatedProgram, @DeleteFlag, @CreatedBy, CURRENT_TIMESTAMP, @UpdatedBy, CURRENT_TIMESTAMP 
                );";

            LogHelper.Debug($"[SQL - InsertBudgetAsync]\nParam: {JsonSerializer.Serialize((object)param)}");
            return await conn.ExecuteAsync(sql, (object)param);
        }

        // ---------------------------------------------------------
        // FY350_12: 排他データの削除
        // ---------------------------------------------------------
        public async Task<int> UnlockAsync(IDbConnection conn, string taNo)
        {
            string sql = "DELETE FROM t_haitaseigyo WHERE tano = @TaNo;";
            return await conn.ExecuteAsync(sql, new { TaNo = taNo });
        }

        // ---------------------------------------------------------
        // FY350_14: 人件費データフラグの更新
        // ---------------------------------------------------------
        public async Task<int> UpdatePersonnelExpenseFlagAsync(IDbConnection conn, dynamic param)
        {
            string sql = @"
                UPDATE yt_yosan 
                SET 
                    flag_2 = '0', 
                    updated_program = @UpdatedProgram, 
                    updated_by = @UpdatedBy, 
                    updated_at = CURRENT_TIMESTAMP 
                WHERE 
                    fiscal_year = @FiscalYear 
                    AND org_code = @OrgCode 
                    AND data_type = '10' 
                    AND mgmt_level = '010' 
                    AND data_category = '1' 
                    AND account_expense_type = '1' 
                    AND account_code IN ('SH0280', 'SH0290', 'SH0300', 'SH0310', 'SH0320', 'SH0330', 'SH0340', 'SH0350', 'SH0360', 'SH0370');";

            LogHelper.Debug($"[SQL - UpdatePersonnelExpenseFlagAsync]\nParam: {JsonSerializer.Serialize((object)param)}");
            return await conn.ExecuteAsync(sql, (object)param);
        }
    }
}