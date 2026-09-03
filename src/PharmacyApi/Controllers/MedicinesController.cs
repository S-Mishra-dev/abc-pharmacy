using Microsoft.AspNetCore.Mvc;
using PharmacyApi.Models;
using PharmacyApi.Services;

namespace PharmacyApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MedicinesController(IMedicineRepository repository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Medicine>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<Medicine>>> GetAll(CancellationToken cancellationToken)
    {
        var medicines = await repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return Ok(medicines);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Medicine), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Medicine>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var medicine = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (medicine is null)
        {
            return NotFound(new { message = $"Medicine with id '{id}' was not found." });
        }

        return Ok(medicine);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Medicine), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Medicine>> Create(
        [FromBody] CreateMedicineRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (HasMoreThanTwoDecimalPlaces(request.Price))
        {
            ModelState.AddModelError(nameof(request.Price), "Price must have at most 2 decimal places.");
            return ValidationProblem(ModelState);
        }

        var medicine = await repository.AddAsync(request, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetById), new { id = medicine.Id }, medicine);
    }

    [HttpPost("{id:guid}/sell")]
    [ProducesResponseType(typeof(SellMedicineResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SellMedicineResponse>> Sell(
        Guid id,
        [FromBody] SellMedicineRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var result = await repository.SellAsync(id, request.Quantity, cancellationToken).ConfigureAwait(false);
            if (result is null)
            {
                return NotFound(new { message = $"Medicine with id '{id}' was not found." });
            }

            var (medicine, sale) = result.Value;
            return Ok(new SellMedicineResponse(medicine, sale));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    private static bool HasMoreThanTwoDecimalPlaces(decimal value)
    {
        return decimal.Round(value, 2) != value;
    }
}

public sealed record SellMedicineResponse(Medicine Medicine, SaleRecord Sale);