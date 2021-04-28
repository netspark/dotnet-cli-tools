        [HttpGet("RoutePlaceholder")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesDefaultResponseType(typeof(Result))]
        public async Task<ActionResult<SuccessResult<VmPlaceholder>>> QueryPlaceholderAction([FromRoute] QueryPlaceholderQuery query)
        {
            var vm = await Mediator.Send(query);

            return Ok(vm);
        }

