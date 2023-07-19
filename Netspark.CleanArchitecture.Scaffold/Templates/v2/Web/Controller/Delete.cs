    [HttpDelete("RoutePlaceholder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerRequestExample(typeof(CommandPlaceholderCommand), typeof(CommandPlaceholderCommandExample))]
    public async Task<ActionResult> CommandPlaceholderAction([FromRoute] CommandPlaceholderCommand command)
    {
        await _mediator.Send(command);

        return NoContent();
    }

