using Microsoft.AspNetCore.Mvc;
using OYA_Web_Application.Models;
using OYA_Web_Application.Models.Services;

namespace OYA_Web_Application.Controllers
{
    public class DatabaseEntryController : Controller
    {
        private readonly DatabaseEntryRepository _repository;
        private readonly ILogger<DatabaseEntryController> _logger;

        public DatabaseEntryController(
            DatabaseEntryRepository repository,
            ILogger<DatabaseEntryController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var distributors =
                    await _repository.GetAllAsync();

                return View(distributors);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error loading distributor list.");

                ViewBag.Error =
                    "Unable to load distributors.";

                return View(
                    new List<DatabaseEntryModel>());
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            var distributor = new DatabaseEntryModel
            {
                IsActive = true
            };

            return View(distributor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            DatabaseEntryModel distributor)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x =>
                        x.Value != null &&
                        x.Value.Errors.Count > 0)
                    .SelectMany(x =>
                        x.Value!.Errors.Select(error =>
                            string.IsNullOrWhiteSpace(
                                error.ErrorMessage)
                                ? error.Exception?.Message
                                : error.ErrorMessage))
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))
                    .ToList();

                ViewBag.Error =
                    string.Join(" | ", errors);

                return View(distributor);
            }

            try
            {
                if (string.IsNullOrWhiteSpace(
                    distributor.Customer))
                {
                    ModelState.AddModelError(
                        "Customer",
                        "Customer is required.");

                    ViewBag.Error =
                        "Customer is required.";

                    return View(distributor);
                }

                if (string.IsNullOrWhiteSpace(
                    distributor.Name))
                {
                    ModelState.AddModelError(
                        "Name",
                        "Distributor name is required.");

                    ViewBag.Error =
                        "Distributor name is required.";

                    return View(distributor);
                }

                var inserted =
                    await _repository.InsertAsync(
                        distributor);

                if (!inserted)
                {
                    ViewBag.Error =
                        "Distributor could not be saved.";

                    return View(distributor);
                }

                TempData["Success"] =
                    "Distributor saved successfully.";

                return RedirectToAction(
                    nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating distributor.");

                ViewBag.Error =
                    "Error saving distributor: " +
                    ex.Message;

                return View(distributor);
            }
        }
    }
}