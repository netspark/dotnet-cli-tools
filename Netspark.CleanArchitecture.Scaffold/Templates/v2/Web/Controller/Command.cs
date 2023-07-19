    [HttpPost("RoutePlaceholder")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerRequestExample(typeof(CommandPlaceholderCommand), typeof(CommandPlaceholderCommandExample))]
    public async Task<ActionResult> CommandPlaceholderAction([FromBody] CommandPlaceholderCommand command)
    {
        await _mediator.Send(command);

        return Ok();
    }

