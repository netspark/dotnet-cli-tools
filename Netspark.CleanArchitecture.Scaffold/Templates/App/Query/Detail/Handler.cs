﻿using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using ApplicationNsPlaceholder.Common.Exceptions;
using ApplicationNsPlaceholder.Common.Interfaces;
using MediatR;

namespace NamespacePlaceholder
{
    public class HandlerPlaceholder : IRequestHandler<QueryPlaceholder, VmPlaceholder>
    {
        private readonly DbContextPlaceholder _context;
        private readonly IMapper _mapper;

        public HandlerPlaceholder(DbContextPlaceholder context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<VmPlaceholder> Handle(QueryPlaceholder request, CancellationToken cancellationToken)
        {
            // TODO: implement query code here
            
            return new VmPlaceholder();
        }
    }
}
