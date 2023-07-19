        [HttpDelete("RoutePlaceholder")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType(typeof(Result))]
        public async Task<ActionResult> CommandPlaceholderAction([FromRoute] CommandPlaceholderCommand command)
        {
            await Mediator.Send(command);

            return NoContent();
        }

