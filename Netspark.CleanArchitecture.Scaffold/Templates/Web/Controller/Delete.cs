        [HttpDelete("RoutePlaceholder")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> CommandPlaceholderAction([FromRoute] CommandPlaceholderCommand command)
        {
            await Mediator.Send(command);

            return NoContent();
        }

