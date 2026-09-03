using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PharmacyApi.Controllers;
using PharmacyApi.Models;
using PharmacyApi.Services;

namespace PharmacyApi.Tests.Controllers;

public sealed class MedicinesControllerTests
{
    private readonly Mock<IMedicineRepository> _repositoryMock;
    private readonly MedicinesController _controller;

    public MedicinesControllerTests()
    {
        _repositoryMock = new Mock<IMedicineRepository>(MockBehavior.Strict);
        _controller = new MedicinesController(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithMedicines()
    {
        List<Medicine> medicines =
        [
            CreateMedicine(Guid.NewGuid(), "Aspirin", quantity: 50),
            CreateMedicine(Guid.NewGuid(), "Ibuprofen", quantity: 20)
        ];

        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(medicines);

        ActionResult<IReadOnlyList<Medicine>> result = await _controller.GetAll(CancellationToken.None);

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result.Result);
        IReadOnlyList<Medicine> payload = Assert.IsAssignableFrom<IReadOnlyList<Medicine>>(okResult.Value);
        Assert.Equal(2, payload.Count);
        Assert.Equal(medicines, payload);
        _repositoryMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAll_WhenEmpty_ReturnsOkWithEmptyCollection()
    {
        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Medicine>());

        ActionResult<IReadOnlyList<Medicine>> result = await _controller.GetAll(CancellationToken.None);

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result.Result);
        IReadOnlyList<Medicine> payload = Assert.IsAssignableFrom<IReadOnlyList<Medicine>>(okResult.Value);
        Assert.Empty(payload);
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsOkWithMedicine()
    {
        Guid id = Guid.NewGuid();
        Medicine medicine = CreateMedicine(id, "Paracetamol", quantity: 15);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(medicine);

        ActionResult<Medicine> result = await _controller.GetById(id, CancellationToken.None);

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result.Result);
        Medicine payload = Assert.IsType<Medicine>(okResult.Value);
        Assert.Equal(id, payload.Id);
        Assert.Equal("Paracetamol", payload.FullName);
        _repositoryMock.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        Guid id = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Medicine?)null);

        ActionResult<Medicine> result = await _controller.GetById(id, CancellationToken.None);

        NotFoundObjectResult notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFound.Value);
        _repositoryMock.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WhenValid_ReturnsCreatedAtAction()
    {
        CreateMedicineRequest request = new()
        {
            FullName = "Amoxicillin",
            Notes = "Take with food",
            ExpiryDate = new DateOnly(2027, 6, 30),
            Quantity = 100,
            Price = 12.50m,
            Brand = "PharmaCo"
        };

        Medicine created = CreateMedicine(
            Guid.NewGuid(),
            request.FullName,
            request.Quantity,
            request.Price,
            request.Brand,
            request.Notes,
            request.ExpiryDate);

        _repositoryMock
            .Setup(r => r.AddAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        ActionResult<Medicine> result = await _controller.Create(request, CancellationToken.None);

        CreatedAtActionResult createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(MedicinesController.GetById), createdResult.ActionName);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        Assert.NotNull(createdResult.RouteValues);
        Assert.Equal(created.Id, createdResult.RouteValues["id"]);
        Medicine payload = Assert.IsType<Medicine>(createdResult.Value);
        Assert.Equal(created.Id, payload.Id);
        Assert.Equal(request.FullName, payload.FullName);
        _repositoryMock.Verify(r => r.AddAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WhenModelStateInvalid_ReturnsValidationProblem()
    {
        CreateMedicineRequest request = new()
        {
            FullName = string.Empty,
            Brand = "Brand",
            ExpiryDate = new DateOnly(2027, 1, 1),
            Quantity = 1,
            Price = 1.00m
        };

        _controller.ModelState.AddModelError(nameof(CreateMedicineRequest.FullName), "Full name is required.");

        ActionResult<Medicine> result = await _controller.Create(request, CancellationToken.None);

        ObjectResult objectResult = Assert.IsType<ObjectResult>(result.Result);
        ValidationProblemDetails problem = Assert.IsType<ValidationProblemDetails>(objectResult.Value);
        Assert.True(problem.Errors.ContainsKey(nameof(CreateMedicineRequest.FullName)));
        _repositoryMock.Verify(
            r => r.AddAsync(It.IsAny<CreateMedicineRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Create_WhenPriceHasMoreThanTwoDecimalPlaces_ReturnsValidationProblem()
    {
        CreateMedicineRequest request = new()
        {
            FullName = "Test Med",
            Notes = string.Empty,
            ExpiryDate = new DateOnly(2027, 1, 1),
            Quantity = 10,
            Price = 9.999m,
            Brand = "BrandX"
        };

        ActionResult<Medicine> result = await _controller.Create(request, CancellationToken.None);

        ObjectResult objectResult = Assert.IsType<ObjectResult>(result.Result);
        ValidationProblemDetails problem = Assert.IsType<ValidationProblemDetails>(objectResult.Value);
        Assert.True(problem.Errors.ContainsKey(nameof(CreateMedicineRequest.Price)));
        _repositoryMock.Verify(
            r => r.AddAsync(It.IsAny<CreateMedicineRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Sell_WhenSuccessful_ReturnsOkWithResponse()
    {
        Guid medicineId = Guid.NewGuid();
        SellMedicineRequest request = new() { Quantity = 2 };
        Medicine updated = CreateMedicine(medicineId, "Cough Syrup", quantity: 8, price: 5.00m);
        SaleRecord sale = new(
            Guid.NewGuid(),
            medicineId,
            updated.FullName,
            request.Quantity,
            updated.Price * request.Quantity,
            DateTimeOffset.UtcNow);

        _repositoryMock
            .Setup(r => r.SellAsync(medicineId, request.Quantity, It.IsAny<CancellationToken>()))
            .ReturnsAsync((updated, sale));

        ActionResult<SellMedicineResponse> result = await _controller.Sell(medicineId, request, CancellationToken.None);

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result.Result);
        SellMedicineResponse payload = Assert.IsType<SellMedicineResponse>(okResult.Value);
        Assert.Equal(updated, payload.Medicine);
        Assert.Equal(sale, payload.Sale);
        _repositoryMock.Verify(
            r => r.SellAsync(medicineId, request.Quantity, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Sell_WhenMedicineNotFound_ReturnsNotFound()
    {
        Guid medicineId = Guid.NewGuid();
        SellMedicineRequest request = new() { Quantity = 1 };

        _repositoryMock
            .Setup(r => r.SellAsync(medicineId, request.Quantity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((Medicine Medicine, SaleRecord Sale)?)null);

        ActionResult<SellMedicineResponse> result = await _controller.Sell(medicineId, request, CancellationToken.None);

        NotFoundObjectResult notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFound.Value);
        _repositoryMock.Verify(
            r => r.SellAsync(medicineId, request.Quantity, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Sell_WhenInsufficientStock_ReturnsConflict()
    {
        Guid medicineId = Guid.NewGuid();
        SellMedicineRequest request = new() { Quantity = 50 };
        const string message = "Insufficient stock to complete the sale.";

        _repositoryMock
            .Setup(r => r.SellAsync(medicineId, request.Quantity, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(message));

        ActionResult<SellMedicineResponse> result = await _controller.Sell(medicineId, request, CancellationToken.None);

        ConflictObjectResult conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
        Assert.NotNull(conflict.Value);
    }

    [Fact]
    public async Task Sell_WhenModelStateInvalid_ReturnsValidationProblem()
    {
        Guid medicineId = Guid.NewGuid();
        SellMedicineRequest request = new() { Quantity = 0 };

        _controller.ModelState.AddModelError(
            nameof(SellMedicineRequest.Quantity),
            "Quantity to sell must be at least 1.");

        ActionResult<SellMedicineResponse> result = await _controller.Sell(medicineId, request, CancellationToken.None);

        ObjectResult objectResult = Assert.IsType<ObjectResult>(result.Result);
        ValidationProblemDetails problem = Assert.IsType<ValidationProblemDetails>(objectResult.Value);
        Assert.True(problem.Errors.ContainsKey(nameof(SellMedicineRequest.Quantity)));
        _repositoryMock.Verify(
            r => r.SellAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static Medicine CreateMedicine(
        Guid id,
        string fullName,
        int quantity = 10,
        decimal price = 9.99m,
        string brand = "Generic",
        string notes = "",
        DateOnly? expiryDate = null)
    {
        return new Medicine(
            id,
            fullName,
            notes,
            expiryDate ?? new DateOnly(2027, 12, 31),
            quantity,
            price,
            brand);
    }
}
