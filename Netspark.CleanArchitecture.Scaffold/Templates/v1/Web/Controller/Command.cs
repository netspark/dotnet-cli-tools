        [HttpPost("RoutePlaceholder")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
        [ProducesDefaultResponseType(typeof(Result))]
        public async Task<ActionResult> CommandPlaceholderAction([FromBody] CommandPlaceholderCommand command)
        {
            await Mediator.Send(command);

            return Ok();
        }

