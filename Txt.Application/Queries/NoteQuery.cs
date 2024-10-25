using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Txt.Domain.Entities;
using Txt.Domain.Repositories.Interfaces;
using Txt.Shared.Dtos;
using Txt.Shared.Queries;

namespace Txt.Application.Queries;

public class NoteQueryHandler(INotesModuleRepository notesModuleRepository, IMapper mapper)
    : IRequestHandler<NoteQuery, List<NoteDto>>
{
    public async Task<List<NoteDto>> Handle(NoteQuery request, CancellationToken cancellationToken)
    {
        List<Note> notes = await notesModuleRepository.FindNotesWhere(note =>
            (request.Id == null || note.Id == request.Id)
            && (request.FolderId == null || note.ParentId == request.FolderId)
        ).ToListAsync(cancellationToken: cancellationToken);

        return mapper.Map<List<NoteDto>>(notes);
    }
}