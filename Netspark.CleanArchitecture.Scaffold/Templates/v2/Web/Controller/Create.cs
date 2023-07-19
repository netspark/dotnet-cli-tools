    [HttpPost("RoutePlaceholder")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerRequestExample(typeof(CommandPlaceholderCommand), typeof(CommandPlaceholderCommandExample))]
    public async Task<ActionResult<string>> CommandPlaceholderAction([FromBody] CommandPlaceholderCommand command)
    {
        var id = await _mediator.Send(command);

        return Created(id, "");
    }

