using BudgetManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BudgetManagement.Services.ShopExpenseBudget
{
    public static class ShopExpenseBudgetDisplayKeys
    {
        public const string VariableExpenseTotal = "__SHOP_EXPENSE_VARIABLE_TOTAL";
        public const string FixedExpenseTotal = "__SHOP_EXPENSE_FIXED_TOTAL";
        public const string PersonnelTotal = "__SHOP_EXPENSE_PERSONNEL_TOTAL";
    }

    public sealed class ShopExpenseBudgetCalculationService
    {
        public ShopExpenseBudgetCalculationResult Apply(IEnumerable<MonthlyBudgetData> source)
        {
            var warnings = new List<string>();

            var dict = source
                .GroupBy(x => x.AccountCode)
                .ToDictionary(
                    g => g.Key,
                    g => MergeSameAccount(g)
                );

            // 販売差益② = 販売差益① + 企画R + 見積評価損
            dict["SH0070"] = Linear(
                "SH0070",
                dict,
                warnings,
                ("SH0040", +1),
                ("SH0050", +1),
                ("SH0060", +1)
            );

            // 変動費計 = テナント変動 + 販売代行料 + DL宣伝変動費 + クレジット手数料 + その他
            dict[ShopExpenseBudgetDisplayKeys.VariableExpenseTotal] = Linear(
                ShopExpenseBudgetDisplayKeys.VariableExpenseTotal,
                dict,
                warnings,
                ("SH0080", +1),
                ("SH0090", +1),
                ("SH0100", +1),
                ("SH0110", +1),
                ("SH0120", +1)
            );

            // 限界利益 = 販売差益② - 変動費計
            dict["SH0130"] = Linear(
                "SH0130",
                dict,
                warnings,
                ("SH0070", +1),
                (ShopExpenseBudgetDisplayKeys.VariableExpenseTotal, -1)
            );

            // 人件費計 = SH0280〜SH0370
            dict[ShopExpenseBudgetDisplayKeys.PersonnelTotal] = Linear(
                ShopExpenseBudgetDisplayKeys.PersonnelTotal,
                dict,
                warnings,
                ("SH0280", +1),
                ("SH0290", +1),
                ("SH0300", +1),
                ("SH0310", +1),
                ("SH0320", +1),
                ("SH0330", +1),
                ("SH0340", +1),
                ("SH0350", +1),
                ("SH0360", +1),
                ("SH0370", +1)
            );

            // 人件費 = 人件費計
            dict["SH0140"] = CloneAs(
                "SH0140",
                dict[ShopExpenseBudgetDisplayKeys.PersonnelTotal]
            );

            // 固定費計 = 人件費 + テナント固定費 + 店頭経費 + 減価償却費 + 金利 + その他
            dict[ShopExpenseBudgetDisplayKeys.FixedExpenseTotal] = Linear(
                ShopExpenseBudgetDisplayKeys.FixedExpenseTotal,
                dict,
                warnings,
                ("SH0140", +1),
                ("SH0150", +1),
                ("SH0160", +1),
                ("SH0170", +1),
                ("SH0180", +1),
                ("SH0190", +1)
            );

            // 店舗利益 = 限界利益 - 固定費計
            dict["SH0200"] = Linear(
                "SH0200",
                dict,
                warnings,
                ("SH0130", +1),
                (ShopExpenseBudgetDisplayKeys.FixedExpenseTotal, -1)
            );

            // バックス経費計 = バックス変動 + バックス固定
            dict["SH0230"] = Linear(
                "SH0230",
                dict,
                warnings,
                ("SH0210", +1),
                ("SH0220", +1)
            );

            // 経常利益 = 店舗利益 - バックス経費計
            dict["SH0240"] = Linear(
                "SH0240",
                dict,
                warnings,
                ("SH0200", +1),
                ("SH0230", -1)
            );

            // 貢献度利益 = 経常利益 + バックス経費計 + 企画R
            dict["SH0250"] = Linear(
                "SH0250",
                dict,
                warnings,
                ("SH0240", +1),
                ("SH0230", +1),
                ("SH0050", +1)
            );

            // 損益分岐点 = (固定費計 + バックス固定) / ((限界利益 - バックス変動) / 純売上)
            dict["SH0260"] = CalculateBreakEvenPoint(dict, warnings);

            // 分岐点差額 = 純売上 - 損益分岐点
            dict["SH0270"] = Linear(
                "SH0270",
                dict,
                warnings,
                ("SH0030", +1),
                ("SH0260", -1)
            );

            return new ShopExpenseBudgetCalculationResult
            {
                Items = dict.Values.ToList(),
                Warnings = warnings
            };
        }

        private static MonthlyBudgetData Linear(
            string targetAccountCode,
            Dictionary<string, MonthlyBudgetData> dict,
            List<string> warnings,
            params (string Code, decimal Sign)[] terms)
        {
            var result = new MonthlyBudgetData
            {
                AccountCode = targetAccountCode
            };

            foreach (var term in terms)
            {
                var source = GetOrZero(dict, term.Code, targetAccountCode, warnings);
                AddMonthly(result, source, term.Sign);
            }

            return result;
        }

        private static MonthlyBudgetData CalculateBreakEvenPoint(
            Dictionary<string, MonthlyBudgetData> dict,
            List<string> warnings)
        {
            var fixedTotal = GetOrZero(dict, ShopExpenseBudgetDisplayKeys.FixedExpenseTotal, "SH0260", warnings);
            var backsFixed = GetOrZero(dict, "SH0220", "SH0260", warnings);
            var marginalProfit = GetOrZero(dict, "SH0130", "SH0260", warnings);
            var backsVariable = GetOrZero(dict, "SH0210", "SH0260", warnings);
            var netSales = GetOrZero(dict, "SH0030", "SH0260", warnings);

            return new MonthlyBudgetData
            {
                AccountCode = "SH0260",

                Month04 = BreakEven(fixedTotal.Month04, backsFixed.Month04, marginalProfit.Month04, backsVariable.Month04, netSales.Month04),
                Month05 = BreakEven(fixedTotal.Month05, backsFixed.Month05, marginalProfit.Month05, backsVariable.Month05, netSales.Month05),
                Month06 = BreakEven(fixedTotal.Month06, backsFixed.Month06, marginalProfit.Month06, backsVariable.Month06, netSales.Month06),
                Month07 = BreakEven(fixedTotal.Month07, backsFixed.Month07, marginalProfit.Month07, backsVariable.Month07, netSales.Month07),
                Month08 = BreakEven(fixedTotal.Month08, backsFixed.Month08, marginalProfit.Month08, backsVariable.Month08, netSales.Month08),
                Month09 = BreakEven(fixedTotal.Month09, backsFixed.Month09, marginalProfit.Month09, backsVariable.Month09, netSales.Month09),

                Month10 = BreakEven(fixedTotal.Month10, backsFixed.Month10, marginalProfit.Month10, backsVariable.Month10, netSales.Month10),
                Month11 = BreakEven(fixedTotal.Month11, backsFixed.Month11, marginalProfit.Month11, backsVariable.Month11, netSales.Month11),
                Month12 = BreakEven(fixedTotal.Month12, backsFixed.Month12, marginalProfit.Month12, backsVariable.Month12, netSales.Month12),
                Month01 = BreakEven(fixedTotal.Month01, backsFixed.Month01, marginalProfit.Month01, backsVariable.Month01, netSales.Month01),
                Month02 = BreakEven(fixedTotal.Month02, backsFixed.Month02, marginalProfit.Month02, backsVariable.Month02, netSales.Month02),
                Month03 = BreakEven(fixedTotal.Month03, backsFixed.Month03, marginalProfit.Month03, backsVariable.Month03, netSales.Month03)
            };
        }

        private static decimal BreakEven(
            decimal fixedTotal,
            decimal backsFixed,
            decimal marginalProfit,
            decimal backsVariable,
            decimal netSales)
        {
            var adjustedMarginalProfit = marginalProfit - backsVariable;

            if (netSales == 0m || adjustedMarginalProfit == 0m)
            {
                return 0m;
            }

            var marginalProfitRate = adjustedMarginalProfit / netSales;

            if (marginalProfitRate == 0m)
            {
                return 0m;
            }

            return (fixedTotal + backsFixed) / marginalProfitRate;
        }

        private static MonthlyBudgetData GetOrZero(
            Dictionary<string, MonthlyBudgetData> dict,
            string accountCode,
            string targetAccountCode,
            List<string> warnings)
        {
            if (dict.TryGetValue(accountCode, out var item))
            {
                return item;
            }

            warnings.Add($"{targetAccountCode} の計算で参照する {accountCode} が存在しません。0として計算しました。");

            return new MonthlyBudgetData
            {
                AccountCode = accountCode
            };
        }

        private static MonthlyBudgetData MergeSameAccount(IEnumerable<MonthlyBudgetData> items)
        {
            var result = new MonthlyBudgetData();

            foreach (var item in items)
            {
                result.AccountCode = item.AccountCode;
                AddMonthly(result, item, +1);
            }

            return result;
        }

        private static MonthlyBudgetData CloneAs(string accountCode, MonthlyBudgetData source)
        {
            return new MonthlyBudgetData
            {
                AccountCode = accountCode,

                Month04 = source.Month04,
                Month05 = source.Month05,
                Month06 = source.Month06,
                Month07 = source.Month07,
                Month08 = source.Month08,
                Month09 = source.Month09,

                Month10 = source.Month10,
                Month11 = source.Month11,
                Month12 = source.Month12,
                Month01 = source.Month01,
                Month02 = source.Month02,
                Month03 = source.Month03
            };
        }

        private static void AddMonthly(MonthlyBudgetData target, MonthlyBudgetData source, decimal sign)
        {
            target.Month04 += source.Month04 * sign;
            target.Month05 += source.Month05 * sign;
            target.Month06 += source.Month06 * sign;
            target.Month07 += source.Month07 * sign;
            target.Month08 += source.Month08 * sign;
            target.Month09 += source.Month09 * sign;

            target.Month10 += source.Month10 * sign;
            target.Month11 += source.Month11 * sign;
            target.Month12 += source.Month12 * sign;
            target.Month01 += source.Month01 * sign;
            target.Month02 += source.Month02 * sign;
            target.Month03 += source.Month03 * sign;
        }
    }

    public sealed class ShopExpenseBudgetCalculationResult
    {
        public List<MonthlyBudgetData> Items { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}