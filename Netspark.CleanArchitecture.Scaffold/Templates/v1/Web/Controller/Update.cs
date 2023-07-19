        [HttpPut("RoutePlaceholder")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> CommandPlaceholderAction([FromBody] CommandPlaceholderCommand command)
        {
            await Mediator.Send(command);

            return NoContent();
        }

