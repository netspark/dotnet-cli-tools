    [HttpPut("RoutePlaceholder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerRequestExample(typeof(CommandPlaceholderCommand), typeof(CommandPlaceholderCommandExample))]
    public async Task<ActionResult> CommandPlaceholderAction([FromBody] CommandPlaceholderCommand command)
    {
        await _mediator.Send(command);

        return NoContent();
    }
