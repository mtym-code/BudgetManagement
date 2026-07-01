using BudgetManagement.Models;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Text;

namespace BudgetManagement.Repositories
{
    // グリッドに表示・更新するためのモデル定義
    public class BudgetOperationItem
    {
        public string OrgCode { get; set; } = string.Empty;
        public string OrgName { get; set; } = string.Empty;
        public bool IsStaffInputCompleted { get; set; }
        public string StaffHandlerCode { get; set; } = string.Empty;
        public string StaffHandlerName { get; set; } = string.Empty;
        public bool IsSendFlag { get; set; }
        public string SendHandlerCode { get; set; } = string.Empty;
        public string SendHandlerName { get; set; } = string.Empty;
        // 🌟 IsSendDone を削除しました
        public string LastSendDateTime { get; set; } = string.Empty;

        // UPSERT時に使用する裏側（非表示）のキー情報
        public string CompanyCode { get; set; } = string.Empty;
        public string DeptCode { get; set; } = string.Empty;
        public string SectionCode { get; set; } = string.Empty;
        public string StaffCode { get; set; } = string.Empty;
        public int TargetMonth { get; set; } = 99;
    }

    public class OperationMeiItem
    {
        public string NameCode { get; set; } = string.Empty;
        public string AbbreviationName { get; set; } = string.Empty;
    }

    public class BudgetOperationMasterRepository
    {
        public async Task<IEnumerable<OperationMeiItem>> GetCompaniesAsync(IDbConnection conn)
        {
            string sql = "SELECT name_code AS NameCode, abbreviation_name AS AbbreviationName FROM m_mei WHERE name_id = 'SSKIZOKU16' AND name_code != '00000000' ORDER BY name_code";
            return await conn.QueryAsync<OperationMeiItem>(sql);
        }

        public async Task<IEnumerable<OperationMeiItem>> GetDepartmentsAsync(IDbConnection conn)
        {
            string sql = "SELECT name_code AS NameCode, abbreviation_name AS AbbreviationName FROM m_mei WHERE name_id = 'SSKIZOKU20' AND name_code != '00000000' ORDER BY name_code";
            return await conn.QueryAsync<OperationMeiItem>(sql);
        }

        public async Task<IEnumerable<OperationMeiItem>> GetSectionsAsync(IDbConnection conn)
        {
            string sql = "SELECT name_code AS NameCode, abbreviation_name AS AbbreviationName FROM m_mei WHERE name_id = 'SSKIZOKU3' AND name_code != '00000000' ORDER BY name_code";
            return await conn.QueryAsync<OperationMeiItem>(sql);
        }

        public async Task<IEnumerable<BudgetOperationItem>> GetOperationMasterListAsync(
                    IDbConnection conn, string year, string inputScreen, string companyCode, string deptCode, string sectionCode, string targetMonth)
        {
            var sql = new StringBuilder();
            var p = new DynamicParameters();
            p.Add("Year", year);
            p.Add("InputScreen", inputScreen);
            p.Add("CompanyCode", companyCode);

            int tMonth = 99;
            if (!string.IsNullOrEmpty(targetMonth)) int.TryParse(targetMonth.Replace("月", ""), out tMonth);
            p.Add("TargetMonth", tMonth);

            if (inputScreen == "360")
            {
                // FY940_70: 会社レベル (mgmt_level = '070')
                sql.AppendLine(@"
                    SELECT 
                        s.company_code AS OrgCode, m.abbreviation_name AS OrgName,
                        COALESCE(u.staff_input_complete_flag, false) AS IsStaffInputCompleted, u.staff_handler_code AS StaffHandlerCode,
                        (SELECT t.formal_name FROM m_tanto t WHERE t.staff_type = '03' AND t.staff_code = u.staff_handler_code) AS StaffHandlerName,
                        COALESCE(u.send_flag, false) AS IsSendFlag, u.send_handler_code AS SendHandlerCode,
                        (SELECT t.formal_name FROM m_tanto t WHERE t.staff_type = '03' AND t.staff_code = u.send_handler_code) AS SendHandlerName,
                        CASE WHEN u.last_sent_date IS NULL THEN NULL 
                             ELSE TO_CHAR(TO_TIMESTAMP(u.last_sent_date || COALESCE(u.last_sent_time, '000000'), 'YYYYMMDDHH24MISS'), 'YYYY/MM/DD HH24:MI:SS') 
                        END AS LastSendDateTime,
                        s.company_code AS CompanyCode, '99999999' AS DeptCode, '99999999' AS SectionCode, '99999999' AS StaffCode, @TargetMonth AS TargetMonth
                    FROM ym_soshiki s
                    JOIN m_mei m ON m.name_id = 'SSKIZOKU16' AND m.name_code != '00000000' AND m.name_code = s.company_code
                    LEFT JOIN ym_unyou u ON u.input_screen = @InputScreen AND u.fiscal_year = s.fiscal_year AND u.company_code = s.company_code AND u.target_month = @TargetMonth
                    WHERE s.fiscal_year = @Year AND s.mgmt_level = '070' AND s.company_code = @CompanyCode
                    ORDER BY s.company_code;");
            }
            else if (inputScreen == "340")
            {
                // FY940_60: 担当者レベル (mgmt_level = '010')
                sql.AppendLine(@"
                    SELECT DISTINCT -- 🌟 修正: DISTINCTを追加して重複を排除
                        s.staff_code AS OrgCode, t.formal_name AS OrgName,
                        COALESCE(u.staff_input_complete_flag, false) AS IsStaffInputCompleted, u.staff_handler_code AS StaffHandlerCode,
                        (SELECT t2.formal_name FROM m_tanto t2 WHERE t2.staff_type = '03' AND t2.staff_code = u.staff_handler_code) AS StaffHandlerName,
                        COALESCE(u.send_flag, false) AS IsSendFlag, u.send_handler_code AS SendHandlerCode,
                        (SELECT t2.formal_name FROM m_tanto t2 WHERE t2.staff_type = '03' AND t2.staff_code = u.send_handler_code) AS SendHandlerName,
                        CASE WHEN u.last_sent_date IS NULL THEN NULL 
                             ELSE TO_CHAR(TO_TIMESTAMP(u.last_sent_date || COALESCE(u.last_sent_time, '000000'), 'YYYYMMDDHH24MISS'), 'YYYY/MM/DD HH24:MI:SS') 
                        END AS LastSendDateTime,
                        s.company_code AS CompanyCode, '99999999' AS DeptCode, '99999999' AS SectionCode, s.staff_code AS StaffCode, @TargetMonth AS TargetMonth
                    FROM ym_soshiki s
                    JOIN m_tanto t ON t.staff_type = '03' AND t.staff_code = s.staff_code
                    LEFT JOIN ym_unyou u ON u.input_screen = @InputScreen AND u.fiscal_year = s.fiscal_year AND u.company_code = s.company_code AND u.staff_code = s.staff_code AND u.target_month = @TargetMonth
                    WHERE s.fiscal_year = @Year AND s.mgmt_level = '010' AND s.company_code = @CompanyCode");
                if (!string.IsNullOrEmpty(deptCode)) { sql.AppendLine(" AND s.dept_code = @DeptCode "); p.Add("DeptCode", deptCode); }
                if (!string.IsNullOrEmpty(sectionCode)) { sql.AppendLine(" AND s.section_code = @SectionCode "); p.Add("SectionCode", sectionCode); }
                sql.AppendLine(" ORDER BY s.staff_code; ");
            }
            else if (inputScreen == "330")
            {
                // FY940_50: 課レベル (mgmt_level = '020')
                sql.AppendLine(@"
                    SELECT 
                        s.section_code AS OrgCode, m.abbreviation_name AS OrgName,
                        COALESCE(u.staff_input_complete_flag, false) AS IsStaffInputCompleted, u.staff_handler_code AS StaffHandlerCode,
                        (SELECT t.formal_name FROM m_tanto t WHERE t.staff_type = '03' AND t.staff_code = u.staff_handler_code) AS StaffHandlerName,
                        COALESCE(u.send_flag, false) AS IsSendFlag, u.send_handler_code AS SendHandlerCode,
                        (SELECT t.formal_name FROM m_tanto t WHERE t.staff_type = '03' AND t.staff_code = u.send_handler_code) AS SendHandlerName,
                        CASE WHEN u.last_sent_date IS NULL THEN NULL 
                             ELSE TO_CHAR(TO_TIMESTAMP(u.last_sent_date || COALESCE(u.last_sent_time, '000000'), 'YYYYMMDDHH24MISS'), 'YYYY/MM/DD HH24:MI:SS') 
                        END AS LastSendDateTime,
                        -- 🌟 修正: 99 を @TargetMonth に変更
                        s.company_code AS CompanyCode, '99999999' AS DeptCode, s.section_code AS SectionCode, '99999999' AS StaffCode, @TargetMonth AS TargetMonth
                    FROM ym_soshiki s
                    JOIN m_mei m ON m.name_id = 'SSKIZOKU3' AND m.name_code != '00000000' AND m.name_code = s.section_code
                    -- 🌟 修正: u.target_month = @TargetMonth を追加
                    LEFT JOIN ym_unyou u ON u.input_screen = @InputScreen AND u.fiscal_year = s.fiscal_year AND u.company_code = s.company_code AND u.section_code = s.section_code AND u.target_month = @TargetMonth
                    WHERE s.fiscal_year = @Year AND s.mgmt_level = '020' AND s.company_code = @CompanyCode");
                if (!string.IsNullOrEmpty(deptCode)) { sql.AppendLine(" AND s.dept_code = @DeptCode "); p.Add("DeptCode", deptCode); }
                if (!string.IsNullOrEmpty(sectionCode)) { sql.AppendLine(" AND s.section_code = @SectionCode "); p.Add("SectionCode", sectionCode); }
                sql.AppendLine(" ORDER BY s.section_code; ");
            }
            else
            {
                // FY940_40: 部レベル (322, 323など) (mgmt_level = '030')
                sql.AppendLine(@"
                    SELECT 
                        s.dept_code AS OrgCode, m.abbreviation_name AS OrgName,
                        COALESCE(u.staff_input_complete_flag, false) AS IsStaffInputCompleted, u.staff_handler_code AS StaffHandlerCode,
                        (SELECT t.formal_name FROM m_tanto t WHERE t.staff_type = '03' AND t.staff_code = u.staff_handler_code) AS StaffHandlerName,
                        COALESCE(u.send_flag, false) AS IsSendFlag, u.send_handler_code AS SendHandlerCode,
                        (SELECT t.formal_name FROM m_tanto t WHERE t.staff_type = '03' AND t.staff_code = u.send_handler_code) AS SendHandlerName,
                        CASE WHEN u.last_sent_date IS NULL THEN NULL 
                             ELSE TO_CHAR(TO_TIMESTAMP(u.last_sent_date || COALESCE(u.last_sent_time, '000000'), 'YYYYMMDDHH24MISS'), 'YYYY/MM/DD HH24:MI:SS') 
                        END AS LastSendDateTime,
                        -- 🌟 修正: 99 を @TargetMonth に変更
                        s.company_code AS CompanyCode, s.dept_code AS DeptCode, '99999999' AS SectionCode, '99999999' AS StaffCode, @TargetMonth AS TargetMonth
                    FROM ym_soshiki s
                    JOIN m_mei m ON m.name_id = 'SSKIZOKU20' AND m.name_code != '00000000' AND m.name_code = s.dept_code
                    -- 🌟 修正: u.target_month = @TargetMonth を追加
                    LEFT JOIN ym_unyou u ON u.input_screen = @InputScreen AND u.fiscal_year = s.fiscal_year AND u.company_code = s.company_code AND u.dept_code = s.dept_code AND u.target_month = @TargetMonth
                    WHERE s.fiscal_year = @Year AND s.mgmt_level = '030' AND s.company_code = @CompanyCode");
                if (!string.IsNullOrEmpty(deptCode)) { sql.AppendLine(" AND s.dept_code = @DeptCode "); p.Add("DeptCode", deptCode); }
                sql.AppendLine(" ORDER BY s.dept_code; ");
            }

            return await conn.QueryAsync<BudgetOperationItem>(sql.ToString(), p);
        }

        public async Task UpdateOperationMasterAsync(IDbConnection conn, string year, string inputScreen, BudgetOperationItem item, string userId)
        {
            string sql = @"
                INSERT INTO ym_unyou (
                    input_screen, fiscal_year, company_code, dept_code, section_code, staff_code, target_month,
                    staff_input_complete_flag, staff_handler_code, 
                    send_flag, send_handler_code,
                    delete_flag, created_by, created_at, updated_by, updated_at
                ) VALUES (
                    @InputScreen, @Year, @CompanyCode, @DeptCode, @SectionCode, @StaffCode, @TargetMonth,
                    @IsStaffInputCompleted, @StaffHandlerCode,
                    @IsSendFlag, @SendHandlerCode,
                    false, @UserId, CURRENT_TIMESTAMP, @UserId, CURRENT_TIMESTAMP
                )
                ON CONFLICT (input_screen, fiscal_year, company_code, dept_code, section_code, staff_code, target_month)
                DO UPDATE SET
                    staff_input_complete_flag = EXCLUDED.staff_input_complete_flag,
                    staff_handler_code        = EXCLUDED.staff_handler_code,
                    send_flag                 = EXCLUDED.send_flag,
                    send_handler_code         = EXCLUDED.send_handler_code,
                    updated_by                = EXCLUDED.updated_by,
                    updated_at                = CURRENT_TIMESTAMP;";

            var param = new
            {
                InputScreen = inputScreen,
                Year = year,
                CompanyCode = item.CompanyCode,
                DeptCode = item.DeptCode,
                SectionCode = item.SectionCode,
                StaffCode = item.StaffCode,
                TargetMonth = item.TargetMonth,
                IsStaffInputCompleted = item.IsStaffInputCompleted,
                StaffHandlerCode = item.IsStaffInputCompleted ? userId : null,
                IsSendFlag = item.IsSendFlag,
                SendHandlerCode = item.IsSendFlag ? userId : null,
                UserId = userId
            };

            await conn.ExecuteAsync(sql, param);
        }
    }
}