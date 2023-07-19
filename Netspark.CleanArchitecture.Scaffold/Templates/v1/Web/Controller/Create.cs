        [HttpPost("RoutePlaceholder")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
        [ProducesDefaultResponseType(typeof(Result))]
        public async Task<ActionResult<SuccessResult<int>>> CommandPlaceholderAction([FromBody] CommandPlaceholderCommand command)
        {
            var id = await Mediator.Send(command);

            return Ok(id);
        }

