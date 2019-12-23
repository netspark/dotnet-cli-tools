        [HttpPost("RoutePlaceholder")]
        public async Task<ActionResult<int>> CommandPlaceholderAction([FromBody] CommandPlaceholderCommand command)
        {
            var id = await Mediator.Send(command);

            return Ok(id);
        }

