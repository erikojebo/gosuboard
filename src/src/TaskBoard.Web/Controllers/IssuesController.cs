﻿using TaskBoard.Web.Controllers.RealTime;
using TaskBoard.Web.Controllers.Results;
using TaskBoard.Web.Entities;
using TaskBoard.Web.Infrastructure;
using TaskBoard.Web.Mapping;
using TaskBoard.Web.Models;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.SignalR.Infrastructure;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace TaskBoard.Web.Controllers
{
    [Route("api/issues")]
    public class IssuesController : BaseController
    {
        private IConnectionManager _connectionManager;
        private BoardContext _context;

        public IssuesController(IConnectionManager connectionManager, BoardContext boardContext)
            : base(boardContext)
        {
            _connectionManager = connectionManager;
            _context = boardContext;
        }

        [HttpGet(Name = "Issues")]
        public CollectionModel<IssueModel> Get()
        {
            var href = Url.Link("Issues", null);
            return GetCollection(href);
        }

        [HttpGet("{id}", Name = "IssueById")]
        public IActionResult Get(int id)
        {
            var issue = _context.Issues.FirstOrDefault(x => x.Id == id);

            if (issue == null)
                return HttpNotFound();

            return Result.Object(issue);
        }

        // GET api/values/5
        [HttpGet("~/api/boards/{boardId}/issues", Name = "IssuesByBoardId")]
        public CollectionModel<IssueModel> GetByBoardId(int boardId)
        {
            var href = Url.Link("IssuesByBoardId", new { boardId });
            return GetCollection(href, x => x.BoardId == boardId);
        }

        [HttpPost("~/api/boards/{boardId}/issues")]
        public IActionResult Post(int boardId, int? stateId, string title)
        {
            var issue = new Issue
            {
                Title = title,
                BoardId = boardId,
                StateId = stateId
            };

            _context.Issues.Add(issue);
            _context.SaveChanges();

            var issueModel = Result.ToModel(issue);
            _connectionManager.BroadcastAddIssue(issueModel);
            
            return Result.Created(issue);
        }

        [HttpPost("~/api/boards/{boardId}/issues/{issueId}/statetransitions", Name = "IssueStateTransitions")]
        public IActionResult Post(int boardId, int issueId, int stateId)
        {
            var issue = _context.Issues.FirstOrDefault(x => x.Id == issueId);

            if (issue == null)
                return HttpNotFound();

            issue.StateId = stateId;

            _context.SaveChanges();

            return new NoContentResult();
        }

        [HttpPut("{id}")]
        public IActionResult Put(int id, string title, string description)
        {
            var issue = _context.Issues.FirstOrDefault(x => x.Id == id);

            if (issue == null)
                return HttpNotFound();

            issue.Title = title;
            issue.Description = description;

            _context.SaveChanges();

            var issueModel = Result.ToModel(issue);
            _connectionManager.BroadcastUpdateIssue(issueModel);

            return Result.Object(issue);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            return DeleteById<Issue>(id, x =>
            {
                var issueModel = Result.ToModel(x);
                _connectionManager.BroadcastDeleteIssue(issueModel);
            });
        }

        private CollectionModel<IssueModel> GetCollection(string href, Expression<Func<Issue, bool>> predicate = null)
        {
            IQueryable<Issue> issues = _context.Issues;

            if (predicate != null)
                issues = issues.Where(predicate);

            return Result.Collection(href, issues);
        }
        private IssueModelResultFactory Result
        {
            get { return new IssueModelResultFactory(Url); }
        }
    }
}
