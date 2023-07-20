    [HttpGet("RoutePlaceholder")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [SwaggerRequestExample(typeof(QueryPlaceholderQuery), typeof(QueryPlaceholderQueryExample))]
    [SwaggerRequestExample(typeof(VmPlaceholder), typeof(VmPlaceholderExample))]
    public async Task<ActionResult<VmPlaceholder>> ActionPlaceholder([FromRoute] QueryPlaceholderQuery query)
    {
        var vm = await _mediator.Send(query);

        return Ok(vm);
    }

