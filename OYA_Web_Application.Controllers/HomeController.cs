using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using OYA_Web_Application.Hubs;
using OYA_Web_Application.Models;
using OYA_Web_Application.Models.Data.Dashboard;
using OYA_Web_Application.Models.Data.QBSync;
using OYA_Web_Application.Models.ViewModel;

using OYA_Web_Application.Services.ReportServices;


namespace OYA_Web_Application.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly IConfiguration _configuration;

        private readonly IWebHostEnvironment _webHostEnvironment;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public HomeController(
            IConfiguration configuration,
            ILogger<HomeController> logger,
            IWebHostEnvironment webHostEnvironment)
        {
            _configuration = configuration;

            _logger = logger;

            _webHostEnvironment = webHostEnvironment;
        }



        // =========================================================
        // DASHBOARD
        // =========================================================

        public IActionResult Dashboard()
        {
            try
            {
                if (_webHostEnvironment.IsDevelopment() &&
                    string.IsNullOrWhiteSpace(_configuration.GetConnectionString("DefaultConnection")))
                {
                    return View(new DashboardVM
                    {
                        dashboardSales = new DashboardSales(),
                        customerSales = new List<CustomerSales>(),
                        oyaMonthlySales = new List<MonthlySales>(),
                        oyaYearlySales = new List<YearlySales>(),
                        pinoMonthlySales = new List<MonthlySales>(),
                        pinoYearlySales = new List<YearlySales>()
                    });
                }

                SalesBifurcation salesDashboard =
                    new SalesBifurcation(_configuration);


                DashboardVM dashboard =
                    new DashboardVM
                    {
                        dashboardSales =
                            salesDashboard.GetOverallDashboardSales(),

                        customerSales =
                            salesDashboard.GetTopCustomerSales(),

                        oyaMonthlySales =
                            salesDashboard.GetOYAMonthlySales(),

                        oyaYearlySales =
                            salesDashboard.GetOYAYearlySales(),

                        pinoMonthlySales =
                            salesDashboard.GetPinoMonthlySales(),

                        pinoYearlySales =
                            salesDashboard.GetPinoYearlySales()
                    };


                return View(dashboard);
            }
            catch (Exception ex)
            {
                if (_webHostEnvironment.IsDevelopment())
                {
                    // Keep local testing usable when the configured SQL database is
                    // unavailable. Production still surfaces the actual failure.
                    Console.Error.WriteLine(
                        $"Dashboard data unavailable: {ex.GetType().Name}: {ex.Message}"
                    );
                    ViewBag.DashboardDataWarning =
                        "Dashboard data could not be loaded. Check the local database connection; showing an empty dashboard for testing.";

                    return View(new DashboardVM
                    {
                        dashboardSales = new DashboardSales(),
                        customerSales = new List<CustomerSales>(),
                        oyaMonthlySales = new List<MonthlySales>(),
                        oyaYearlySales = new List<YearlySales>(),
                        pinoMonthlySales = new List<MonthlySales>(),
                        pinoYearlySales = new List<YearlySales>()
                    });
                }

                _logger.LogError(ex, "Error loading Dashboard.");
                throw;
            }
        }



        // =========================================================
        // LOGS
        // =========================================================

        public IActionResult Logs()
        {
            return View();
        }



        // =========================================================
        // USERS
        // =========================================================

        public IActionResult Users()
        {
            return View();
        }



        // =========================================================
        // REPORTS
        // =========================================================

        public IActionResult Reports()
        {
            try
            {
                SalesBifurcation salesDashboard =
                    new SalesBifurcation(_configuration);


                DailyReports dailyReports =
                    new DailyReports(_configuration);


                ViewBag.Contractors =
                    dailyReports.GetContractors();


                return View(
                    salesDashboard.GetOverallDashboardSales()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error loading Reports."
                );

                throw;
            }
        }



        // =========================================================
        // SYNC PAGE
        // =========================================================
        //
        // Loads the current synchronization information
        // from the database.
        //
        // =========================================================

        [HttpGet]
        public IActionResult Sync()
        {
            try
            {
                Console.WriteLine(
                    "=================================="
                );

                Console.WriteLine(
                    "Loading QuickBooks Sync Status"
                );

                Console.WriteLine(
                    "Time: " +
                    DateTime.Now.ToString(
                        "yyyy-MM-dd HH:mm:ss"
                    )
                );

                Console.WriteLine(
                    "=================================="
                );


                SycnServices sycnServices =
                    new SycnServices(_configuration);


                var syncDetails =
                    sycnServices.GetAllSyncDetails();


                return View(syncDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error loading QuickBooks Sync page."
                );


                Console.WriteLine(
                    "SYNC PAGE ERROR"
                );

                Console.WriteLine(
                    ex.ToString()
                );


                throw;
            }
        }



        // =========================================================
        // MANUAL SYNC
        // =========================================================
        //
        // This is called when the user presses:
        //
        //       Start Sync Now
        //
        //
        // It sends:
        //
        //       Channel : OYA_SYNC_UPDATE
        //       Message : start sync
        //
        //
        // QB_Sync_Helias must receive this message and
        // start the actual QuickBooks synchronization.
        //
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> SyncNow()
        {
            try
            {
                Console.WriteLine(
                    "=================================="
                );

                Console.WriteLine(
                    "MANUAL QUICKBOOKS SYNC STARTED"
                );

                Console.WriteLine(
                    "Time: " +
                    DateTime.Now.ToString(
                        "yyyy-MM-dd HH:mm:ss"
                    )
                );

                Console.WriteLine(
                    "=================================="
                );


                // -------------------------------------------------
                // CREATE PUBNUB INSTANCE
                // -------------------------------------------------

                PubNub pNub =
                    new PubNub(_configuration);


                // -------------------------------------------------
                // SEND START SYNC COMMAND
                // -------------------------------------------------

                var message =
                    new Dictionary<string, string>
                    {
                        {
                            "msg",
                            "start sync"
                        }
                    };


                await pNub.SendMessageAsync(message, HttpContext.RequestAborted);


                Console.WriteLine(
                    "=================================="
                );

                Console.WriteLine(
                    "QuickBooks synchronization message sent."
                );

                Console.WriteLine(
                    "Channel : OYA_SYNC_UPDATE"
                );

                Console.WriteLine(
                    "Message : start sync"
                );

                Console.WriteLine(
                    "=================================="
                );


                // -------------------------------------------------
                // CHECK WHETHER THIS WAS AN AJAX REQUEST
                // -------------------------------------------------

                bool isAjaxRequest =
                    Request.Headers["X-Requested-With"]
                        .ToString()
                        .Equals(
                            "XMLHttpRequest",
                            StringComparison.OrdinalIgnoreCase
                        );


                // -------------------------------------------------
                // AJAX RESPONSE
                // -------------------------------------------------

                if (isAjaxRequest)
                {
                    return Json(
                        new
                        {
                            success = true,

                            message =
                                "Sync command accepted by PubNub; waiting for the backend.",

                            startedAt =
                                DateTime.Now.ToString(
                                    "yyyy-MM-dd HH:mm:ss"
                                )
                        }
                    );
                }


                // -------------------------------------------------
                // NORMAL BROWSER REQUEST
                // -------------------------------------------------

                return RedirectToAction(
                    nameof(Sync)
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "=================================="
                );

                Console.WriteLine(
                    "QUICKBOOKS MANUAL SYNC ERROR"
                );

                Console.WriteLine(
                    ex.ToString()
                );

                Console.WriteLine(
                    "=================================="
                );


                _logger.LogError(
                    ex,
                    "Unable to start QuickBooks manual synchronization."
                );


                bool isAjaxRequest =
                    Request.Headers["X-Requested-With"]
                        .ToString()
                        .Equals(
                            "XMLHttpRequest",
                            StringComparison.OrdinalIgnoreCase
                        );


                if (isAjaxRequest)
                {
                    return StatusCode(
                        500,
                        new
                        {
                            success = false,

                            message =
                                "Unable to start QuickBooks synchronization.",

                            error =
                                ex.Message
                        }
                    );
                }


                throw;
            }
        }



        // =========================================================
        // AUTOMATIC SYNC
        // =========================================================
        //
        // Sync.cshtml will call this endpoint automatically
        // every 1 minute.
        //
        // IMPORTANT:
        //
        // This sends exactly the same PubNub command as
        // the manual SyncNow() method.
        //
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> AutoSync()
        {
            try
            {
                Console.WriteLine(
                    "=================================="
                );

                Console.WriteLine(
                    "AUTOMATIC QUICKBOOKS SYNC STARTED"
                );

                Console.WriteLine(
                    "Time: " +
                    DateTime.Now.ToString(
                        "yyyy-MM-dd HH:mm:ss"
                    )
                );

                Console.WriteLine(
                    "=================================="
                );


                // -------------------------------------------------
                // CREATE PUBNUB INSTANCE
                // -------------------------------------------------

                PubNub pNub =
                    new PubNub(_configuration);


                // -------------------------------------------------
                // SEND START SYNC COMMAND
                // -------------------------------------------------

                var message =
                    new Dictionary<string, string>
                    {
                        {
                            "msg",
                            "start sync"
                        }
                    };


                await pNub.SendMessageAsync(message, HttpContext.RequestAborted);


                Console.WriteLine(
                    "Automatic QuickBooks sync command sent."
                );


                Console.WriteLine(
                    "Channel : OYA_SYNC_UPDATE"
                );

                Console.WriteLine(
                    "Message : start sync"
                );


                return Json(
                    new
                    {
                        success = true,

                        message =
                            "Sync command accepted by PubNub; waiting for the backend.",

                        time =
                            DateTime.Now.ToString(
                                "yyyy-MM-dd HH:mm:ss"
                            )
                    }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "=================================="
                );

                Console.WriteLine(
                    "AUTOMATIC QUICKBOOKS SYNC ERROR"
                );

                Console.WriteLine(
                    ex.ToString()
                );

                Console.WriteLine(
                    "=================================="
                );


                _logger.LogError(
                    ex,
                    "Automatic QuickBooks synchronization failed."
                );


                return StatusCode(
                    500,
                    new
                    {
                        success = false,

                        message =
                            "Unable to start QuickBooks synchronization.",

                        error =
                            ex.Message
                    }
                );
            }
        }



        // =========================================================
        // GET CURRENT SYNC STATUS
        // =========================================================
        //
        // The Sync page can call this endpoint periodically
        // to get the latest database status WITHOUT leaving
        // the page.
        //
        // =========================================================

        [HttpGet]
        public IActionResult SyncStatus()
        {
            try
            {
                SycnServices sycnServices =
                    new SycnServices(_configuration);


                var syncDetails =
                    sycnServices.GetAllSyncDetails();


                return Json(
                    new
                    {
                        success = true,

                        data = syncDetails
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving QuickBooks synchronization status."
                );


                return StatusCode(
                    500,
                    new
                    {
                        success = false,

                        message =
                            "Unable to retrieve synchronization status.",

                        error =
                            ex.Message
                    }
                );
            }
        }



        // =========================================================
        // DOWNLOAD REPORT - POST
        // =========================================================

        [HttpPost]
        public IActionResult DownloadReport(
            IFormCollection frm,
            string reportType,
            string fromDate,
            string toDate)
        {
            string filePath = "";

            string fileName = "";

            string webRootPath =
                _webHostEnvironment.WebRootPath;


            reportType =
                string.IsNullOrEmpty(reportType)
                    ? Convert.ToString(frm["reportType"])
                    : reportType;


            var startVal = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            if (!string.IsNullOrWhiteSpace(fromDate) &&
                !DateTime.TryParse(fromDate, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out startVal))
            {
                return BadRequest("The start date is invalid.");
            }

            var endVal = DateTime.Today;
            if (!string.IsNullOrWhiteSpace(toDate) &&
                !DateTime.TryParse(toDate, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out endVal))
            {
                return BadRequest("The end date is invalid.");
            }

            if (startVal.Date > endVal.Date)
            {
                return BadRequest("The start date must be on or before the end date.");
            }


            switch (reportType)
            {
                case "HeliasDailyReport":
                    {
                        DailyReports dr =
                            new DailyReports(_configuration);


                        fileName =
                            "Helias_Sales_Report";


                        return File(
                            dr.DownloadExcel(),

                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                            fileName +
                            "_" +
                            DateTime.Now.ToShortDateString() +
                            ".xlsx"
                        );
                    }


                case "Territory_Report":
                    {
                        TerritoryReports tr =
                            new TerritoryReports(
                                _configuration
                            );


                        fileName =
                            "Helias Sales Report - Territory";


                        return File(
                            tr.DownloadExcel(),

                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                            fileName +
                            "_" +
                            DateTime.Now.ToShortDateString() +
                            ".xlsx"
                        );
                    }


                case "AR_Aging_Detail_Oya":
                    {
                        AgingReport ar4 =
                            new AgingReport(
                                _configuration
                            );


                        fileName =
                            "Aging Summary OYA";


                        return File(
                            ar4.DownloadExcel(
                                "OYA",
                                "Details"
                            ),

                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                            fileName +
                            "_" +
                            DateTime.Now.ToShortDateString() +
                            ".xlsx"
                        );
                    }


                case "AR_Aging_Detail_Pino":
                    {
                        AgingReport ar3 =
                            new AgingReport(
                                _configuration
                            );


                        fileName =
                            "Aging Summary PINO";


                        return File(
                            ar3.DownloadExcel(
                                "PINO",
                                "Details"
                            ),

                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                            fileName +
                            "_" +
                            DateTime.Now.ToShortDateString() +
                            ".xlsx"
                        );
                    }


                case "AR_Aging_Summary_Oya":
                    {
                        AgingReport ar2 =
                            new AgingReport(
                                _configuration
                            );


                        fileName =
                            "Aging Summary OYA";


                        return File(
                            ar2.DownloadExcel(),

                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                            fileName +
                            "_" +
                            DateTime.Now.ToShortDateString() +
                            ".xlsx"
                        );
                    }


                case "AR_Aging_Summary_Pino":
                    {
                        AgingReport ar =
                            new AgingReport(
                                _configuration
                            );


                        fileName =
                            "Aging Summary PINO";


                        return File(
                            ar.DownloadExcel("PINO"),

                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                            fileName +
                            "_" +
                            DateTime.Now.ToShortDateString() +
                            ".xlsx"
                        );
                    }


                default:
                    {
                        var reportParts = reportType?.Split('_', StringSplitOptions.RemoveEmptyEntries);
                        if (reportParts == null || reportParts.Length != 2 || reportParts[0] != "Commission")
                        {
                            return BadRequest("The requested report type is invalid.");
                        }

                        string contractor = reportParts[1];


                        CommissionReport cr =
                            new CommissionReport(
                                _configuration
                            );


                        fileName =
                            "Commission Report - " +
                            contractor;


                        return File(
                            cr.DownloadExcel(
                                contractor,
                                startVal,
                                endVal
                            ),

                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                            fileName +
                            "_" +
                            DateTime.Now.ToShortDateString() +
                            ".xlsx"
                        );
                    }
            }
        }



        // =========================================================
        // DOWNLOAD REPORT - GET
        // =========================================================

        [HttpGet]
        public IActionResult DownloadReport(
            string reportType)
        {
            string filePath = "";

            string fileName = "";

            string webRootPath =
                _webHostEnvironment.WebRootPath;


            switch (reportType)
            {
                case "HeliasDailyReport":
                    {
                        DailyReports dr =
                            new DailyReports(_configuration);


                        fileName =
                            "Helias_Sales_Report";


                        return File(
                            dr.DownloadExcel(),

                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                            fileName +
                            "_" +
                            DateTime.Now.ToShortDateString() +
                            ".xlsx"
                        );
                    }


                case "Territory_Report":
                    {
                        TerritoryReports tr =
                            new TerritoryReports(
                                _configuration
                            );


                        fileName =
                            "Helias Sales Report - Territory";


                        return File(
                            tr.DownloadExcel(),

                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                            fileName +
                            "_" +
                            DateTime.Now.ToShortDateString() +
                            ".xlsx"
                        );
                    }


                case "Territory_International":
                    {
                        InternationalReports ir =
                            new InternationalReports(
                                _configuration
                            );


                        fileName =
                            "Helias Sales Report - International";


                        return File(
                            ir.DownloadExcel(),

                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                            fileName +
                            "_" +
                            DateTime.Now.ToShortDateString() +
                            ".xlsx"
                        );
                    }


                case "AR_Aging_Detail_Oya":
                    {
                        AgingReport ar4 =
                            new AgingReport(
                                _configuration
                            );


                        fileName =
                            "Aging Details OYA";


                        return File(
                            ar4.DownloadExcel(
                                "OYA",
                                "Details"
                            ),

                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                            fileName +
                            "_" +
                            DateTime.Now.ToShortDateString() +
                            ".xlsx"
                        );
                    }


                case "AR_Aging_Detail_Pino":
                    {
                        AgingReport ar3 =
                            new AgingReport(
                                _configuration
                            );


                        fileName =
                            "Aging Details PINO";


                        return File(
                            ar3.DownloadExcel(
                                "PINO",
                                "Details"
                            ),

                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                            fileName +
                            "_" +
                            DateTime.Now.ToShortDateString() +
                            ".xlsx"
                        );
                    }


                case "AR_Aging_Summary_Oya":
                    {
                        AgingReport ar2 =
                            new AgingReport(
                                _configuration
                            );


                        fileName =
                            "Aging Summary OYA";


                        return File(
                            ar2.DownloadExcel(),

                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                            fileName +
                            "_" +
                            DateTime.Now.ToShortDateString() +
                            ".xlsx"
                        );
                    }


                case "AR_Aging_Summary_Pino":
                    {
                        AgingReport ar =
                            new AgingReport(
                                _configuration
                            );


                        fileName =
                            "Aging Summary PINO";


                        return File(
                            ar.DownloadExcel("PINO"),

                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                            fileName +
                            "_" +
                            DateTime.Now.ToShortDateString() +
                            ".xlsx"
                        );
                    }


                default:
                    {
                        string contractor =
                            reportType.Split('_')[1];


                        CommissionReport cr =
                            new CommissionReport(
                                _configuration
                            );


                        fileName =
                            "Commission Report - " +
                            contractor;


                        return File(
                            cr.DownloadExcel(contractor),

                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                            fileName +
                            "_" +
                            DateTime.Now.ToShortDateString() +
                            ".xlsx"
                        );
                    }
            }
        }



        // =========================================================
        // ERROR
        // =========================================================

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true
        )]

        [AllowAnonymous]

        public IActionResult Error()
        {
            return View(
                new ErrorViewModel
                {
                    RequestId =
                        Activity.Current?.Id ??
                        HttpContext.TraceIdentifier
                }
            );
        }
    }
}
