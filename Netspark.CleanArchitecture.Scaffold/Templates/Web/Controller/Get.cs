        [HttpGet("RoutePlaceholder")]
        public async Task<ActionResult<VmPlaceholder>> QueryPlaceholderAction([FromRoute] QueryPlaceholderQuery query)
        {
            var vm = await Mediator.Send(query);

            return Ok(vm);
        }

