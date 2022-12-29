using ExpenseTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Syncfusion.EJ2.Spreadsheet;
using System.Globalization;

namespace ExpenseTracker.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Last 7 Days transactions
            DateTime now = DateTime.Now;
            DateTime StartDate = DateTime.Today.AddDays(-6);
            DateTime EndDate = DateTime.Today;
            DateTime StartDateOfThisMonth = new DateTime(now.Year, now.Month, 1);
            DateTime EndDateOfThisMonth = StartDateOfThisMonth.AddMonths(1).AddDays(-1);
            int numberOfDaysInCurrentMonth = int.Parse(EndDateOfThisMonth.ToString("dd"));

            //Console.WriteLine("uwaga idzie x " + x);

            Console.WriteLine("Start date of today month" + StartDateOfThisMonth);
            Console.WriteLine("End date of today month" + EndDateOfThisMonth);


            List<Transaction> SelectedTransactions = await _context.Transactions
                .Include(x => x.Category)
                .Where(y => y.Date >= StartDate && y.Date <= EndDate)
                .ToListAsync();

            List<Transaction> SelectedTransactionsInLastMonth = await _context.Transactions
                .Include(x => x.Category)
                .Where(y => y.Date >= StartDateOfThisMonth && y.Date <= EndDateOfThisMonth)
                .ToListAsync();

            // Total Income
            int TotalIncome = SelectedTransactionsInLastMonth
                .Where(i => i.Category.Type == "Income")
                .Sum(j => j.Amount);

            ViewBag.TotalIncome = TotalIncome.ToString("C", CultureInfo.CreateSpecificCulture("en-US"));

            Console.WriteLine("Total Income = " + TotalIncome.ToString());


            // Total Expense
            int TotalExpense = SelectedTransactionsInLastMonth
                .Where(i => i.Category.Type == "Expense")
                .Sum(j => j.Amount);

            ViewBag.TotalExpense = TotalExpense.ToString("C", CultureInfo.CreateSpecificCulture("en-US"));

            Console.WriteLine(TotalExpense.ToString());

            // Balance
            int Balance = TotalIncome - TotalExpense;
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            culture.NumberFormat.CurrencyNegativePattern = 1;

            ViewBag.Balance = String.Format(culture, "{0:C0}", Balance);

            // Doughnut Chart - Expense By Category
            ViewBag.DoughnutChartData = SelectedTransactionsInLastMonth
                .Where(i => i.Category.Type == "Expense")
                .GroupBy(j => j.Category.CategoryId)
                .Select(k => new
                {
                    categoryTitleWithIcon = k.First().Category.Icon + " " + k.First().Category.Title,
                    amount = k.Sum(j => j.Amount),
                    formattedAmount = k.Sum(j => j.Amount).ToString("C", culture),
                })
                .OrderByDescending(l => l.amount)
                .ToList();

            // Spline Chart -  Income vs Expense
            // Income
            List<SplineChartData> IncomeSummary = SelectedTransactionsInLastMonth
                .Where(i => i.Category.Type == "Income")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    income = k.Sum(l => l.Amount)
                })
                .ToList();

            // Expense
            List<SplineChartData> ExpenseSummary = SelectedTransactionsInLastMonth
                .Where(i => i.Category.Type == "Expense")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    expense = k.Sum(l => l.Amount)
                })
                .ToList();

            // Combine Income & Expense
            string[] Last31Days = Enumerable.Range(0, numberOfDaysInCurrentMonth)
                .Select(i => StartDateOfThisMonth.AddDays(i).ToString("dd-MMM"))
                .ToArray();

            ViewBag.SplineChartData = from day in Last31Days
                                      join income in IncomeSummary on day equals income.day into dayIncomeJoined
                                      from income in dayIncomeJoined.DefaultIfEmpty()
                                      join expense in ExpenseSummary on day equals expense.day into expenseJoined
                                      from expense in expenseJoined.DefaultIfEmpty()
                                      select new
                                      {
                                          day = day,
                                          income = income == null ? 0 : income.income,
                                          expense = expense == null ? 0 : expense.expense,
                                      };

            // Recent Transactions
            ViewBag.RecentTransactions = await _context.Transactions
                .Include(i => i.Category)
                .OrderByDescending(j => j.Date)
                .Take(5)
                .ToListAsync();

            return View();
        }
    }

    public class SplineChartData
    {
        public string day;
        public int income;
        public int expense;

    }
}
