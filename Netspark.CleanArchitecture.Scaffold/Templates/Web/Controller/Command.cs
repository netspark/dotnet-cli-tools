        [HttpPost("RoutePlaceholder")]
        public async Task<ActionResult> CommandPlaceholderAction([FromBody] CommandPlaceholderCommand command)
        {
            await Mediator.Send(command);

            return Ok();
        }

