using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OYA_Web_Application.Models;
using OYA_Web_Application.Models.Services;

namespace OYA_Web_Application.Controllers;

public class DistributorController : Controller
{
	private readonly DistributorService _service;

	private readonly ILogger<DistributorController> _logger;

	private readonly IConfiguration _configuration;

	private readonly IWebHostEnvironment _webHostEnvironment;

	public DistributorController(IConfiguration configuration, ILogger<DistributorController> logger, IWebHostEnvironment webHostEnvironment)
	{
		_webHostEnvironment = webHostEnvironment;
		_logger = logger;
		_configuration = configuration;
		_service = new DistributorService(configuration);
	}

	public IActionResult Index()
	{
		List<DistributorModel> data = _service.GetAllMaster();
		return View(data);
	}

    [HttpPost]
    public IActionResult SaveMaster([FromBody] DistributorModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Customer))
        {
            return BadRequest("Customer required");
        }

        DistributorModel existing = _service.GetAllMaster()
            .FirstOrDefault(x => x.Customer == model.Customer);

        if (existing == null)
        {
            _service.Insert(model);      // <-- INSERT INTO Distributor
        }
        else
        {
            _service.Update(model);      // <-- UPDATE Distributor
        }

        return Json(new { success = true });
    }

    public IActionResult Mapping()
	{
		List<DistributorModel> data = _service.GetAll();
		return View(data);
	}

	[HttpPost]
	public IActionResult SaveMapping([FromBody] List<DistributorModel> models)
	{
		_service.UpdateMapping(models);
		return Json(new
		{
			success = true
		});
	}

	[HttpPost]
	public IActionResult AddMapping([FromBody] MappingRequest req)
	{
		_service.AddMappingWithShift(req);
		return Json(new
		{
			success = true
		});
	}
}
