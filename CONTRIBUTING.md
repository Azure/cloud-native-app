# Contributing Guidelines

The Cloud Native App project accepts contributions via GitHub pull requests. This document outlines the process
to help get your contribution accepted.

## Reporting a Security Issue

Since the Cloud Native App project is a complication of any CNCF projects, if you encounter a security issue with a CNCF project, please locate that project's GitHub page and follow thier security guidelines.

## Pull request workflow

The following sections describe how to contribute code by opening a pull request.

### 1. Fork the [Cloud Native App](https://github.com/Azure/cloud-native-app) repository

1. Visit `https://github.com/Azure/cloud-native-app`.
1. Click the `Fork` button.

### 2. Clone the new fork to your workstation

Set `GITHUB_USERNAME` to match your Github username:

```
export GITHUB_USERNAME=<github username>
```

Clone and set up your fork:

```sh
git clone git@github.com:$GITHUB_USERNAME/Azure/cloud-native-app.git

cd cloud-native-app
git remote add upstream git@github.com:Azure/cloud-native-app.git

# Block accidental pushes to upstream's main branch
git remote set-url --push upstream no_push

# Verify your remote
git remote -v
```

### 3. Git branch

Get your local `main` branch up to date with upstream's `main` branch:

```sh
git fetch upstream
git checkout main
git rebase upstream/main
```

Create a local branch from `main` for your development:

```sh
# While on the main branch
git checkout -b <branch name>
# ex: git checkout -b feature
```

Keep your branch up to date during development:

```sh
git fetch upstream
git rebase upstream/main

# or: git pull --rebase upstream main
```

### 4. Commit

Make code changes on the `feature` branch and commit them with your signature

```sh
git commit -s
```

Follow the [commit style guidelines](#commit-style-guideline).

Make sure to squash your commits before opening a pull request. This is preferred so that your pull request can be merged as is without requesting to be squashed before merge if possible.

### 5. Push

Push your branch to your remote fork:

```sh
git push -f <remote name> <branch name>
```

### 6. Open a pull request

1. Visit your fork at `https://github.com/$GITHUB_USERNAME/Azure/cloud-native-app.git`.
1. Open a pull request from your `feature` branch using the `Compare & Pull Request` button.
1. Fill the pull request template and provide enough description so that it can be reviewed.

If your pull request is not ready to be reviewed, open it as a draft.

#### Get code reviewed

Your pull request will be reviewed by the maintainers to ensure correctness. Reviewers might approve the pull request or suggest improvements to make before the changes can be committed.

#### Squash commits

Address review comments, squash all commits in the pull request into a single commit and get your branch up to date with upstream's `main` branch before pushing to your remote.

```sh
git fetch upstream
git rebase upstream/main

git push -f
```

### Merging pull requests

Pull requests by default must be merged by a core maintainer using the `Merge pull request` option as a `merge commit`.
Maintainers can add the `automerge` or `autorebase` label to a pull request, additional details [here](docs/automerge.md).

Pull requests will be merged based on the following criteria:

- Has at least two LGTM from a core maintainer.
- Commits in the pull request are squashed and have a valid [signature](#sign-your-work).
- Commits follow the [commit style guidelines](#commit-style-guideline).
- Does not have the `do-not-merge/hold` label.
- Does not have the `wip` label.
- All status checks have succeeded.
- If the person who opened the pull request is a core maintainer, then only that person is expected to merge once it has the necessary LGTMs/reviews. Another maintainer can merge the pull request at their discretion if they feel the pull request must be merged urgently.
- Core maintainers are allowed to use the `squash and merge` option only for their own pull requests.

### Commit Style Guideline

We follow a rough convention for commit messages borrowed from [Deis](https://github.com/deis/deis/blob/master/CONTRIBUTING.md#commit-style-guideline). This is an example of a commit:

```
feat(scripts/test-cluster): add a cluster test command

Adds test experience where there was none before
and resolves #1213243.
```

This is a more formal template:

```
{type}({scope}): {subject}
<BLANK LINE>
{body}
```

The `{scope}` can be anything specifying the place of the commit change. Use `*` to denote multiple areas (i.e. `scripts/*: refactored lots of stuff`).

The `{subject}` needs to use imperative, present tense: `change` not `changed` nor `changes`. The first letter should not be capitalized, and there is no dot (`.`) at the end.

Just like the `{subject}`, the message `{body}` needs to be in the present tense and includes the motivation for the change as well as a contrast with the previous behavior. The first letter in the paragraph must be capitalized. If there is an issue number associate with the chunk of work that should be mentioned in this section as well so that it may be closed once the PR with this commit is merged.

Any line of the commit message cannot be longer than 72 characters, with the subject line limited to 50 characters. This allows the message to be easier to read on GitHub as well as in various git tools.

The allowed types for `{type}` are as follows:

```
feat -> feature
fix -> bug fix
docs -> documentation
style -> formatting
ref -> refactoring code
test -> adding missing tests
chore -> maintenance
```

## Sign Your Work

The sign-off is a simple line at the end of the explanation for a commit. All commits needs to be
signed. Your signature certifies that you wrote the patch or otherwise have the right to contribute
the material. The rules are pretty simple, if you can certify the below (from
[developercertificate.org](https://developercertificate.org/)):

```
Developer Certificate of Origin
Version 1.1

Copyright (C) 2004, 2006 The Linux Foundation and its contributors.
1 Letterman Drive
Suite D4700
San Francisco, CA, 94129

Everyone is permitted to copy and distribute verbatim copies of this
license document, but changing it is not allowed.

Developer's Certificate of Origin 1.1

By making a contribution to this project, I certify that:

(a) The contribution was created in whole or in part by me and I
    have the right to submit it under the open source license
    indicated in the file; or

(b) The contribution is based upon previous work that, to the best
    of my knowledge, is covered under an appropriate open source
    license and I have the right under that license to submit that
    work with modifications, whether created in whole or in part
    by me, under the same open source license (unless I am
    permitted to submit under a different license), as indicated
    in the file; or

(c) The contribution was provided directly to me by some other
    person who certified (a), (b) or (c) and I have not modified
    it.

(d) I understand and agree that this project and the contribution
    are public and that a record of the contribution (including all
    personal information I submit with it, including my sign-off) is
    maintained indefinitely and may be redistributed consistent with
    this project or the open source license(s) involved.
```

Then you just add a line to every git commit message:

    Signed-off-by: Joe Smith <joe.smith@example.com>

Use your real name (sorry, no pseudonyms or anonymous contributions.)

If you set your `user.name` and `user.email` git configs, you can sign your commit automatically
with `git commit -s`.

Note: If your git config information is set properly then viewing the `git log` information for your
commit will look something like this:

```
Author: Joe Smith <joe.smith@example.com>
Date:   Thu Feb 2 11:41:15 2018 -0800

    Update README

    Signed-off-by: Joe Smith <joe.smith@example.com>
```

Notice the `Author` and `Signed-off-by` lines match. If they don't your PR will be rejected by the
automated DCO check.

## Support Channels

For a CNCF project, please locate that project's GitHub page and follow thier support channel and process. For support of the actual CNCF Cloud Native app, please open issues on GitHub.

Before opening a new issue or submitting a new pull request, it's helpful to search the project -
it's likely that another user has already reported the issue you're facing, or it's a known issue
that we're already aware of. It is also worth asking on the Slack channels.

## Milestones

We use milestones to track progress of specific planned releases.

For example, if the latest currently-released version is `3.2.1`, an issue/PR which pertains to a
specific upcoming bugfix or feature release could fall into one of two different active milestones:
`3.2.2` or `3.3.0`.

Issues and PRs which are deemed backwards-incompatible may be added to the discussion items. An issue or PR that we are not
sure we will be addressing will not be added to any milestone.

A milestone (and hence release) can be closed when all outstanding issues/PRs have been closed
or moved to another milestone and the associated release has been published.

## Semantic Versioning

The CNCF Cloud Native App maintains a strong commitment to backward compatibility. All of our changes to protocols and
formats are backward compatible from one major release to the next. No features, flags, or commands
are removed or substantially modified (unless we need to fix a security issue).

## Issues

Issues are used as the primary method for tracking anything to do with the CNCF Cloud Native App project.

### Issue Types

There are 5 types of issues (each with their own corresponding [label](#labels)):

- `question/support`: These are support or functionality inquiries that we want to have a record of
  for future reference. Generally these are questions that are too complex or large to store in the
  Slack channel or have particular interest to the community as a whole. Depending on the
  discussion, these can turn into `feature` or `bug` issues.
- `proposal`: Used for items (like this one) that propose a new ideas or functionality that require
  a larger community discussion. This allows for feedback from others in the community before a
  feature is actually developed. This is not needed for small additions. Final word on whether or
  not a feature needs a proposal is up to the core maintainers. All issues that are proposals should
  both have a label and an issue title of "Proposal: [the rest of the title]." A proposal can become
  a `feature` and does not require a milestone.
- `feature`: These track specific feature requests and ideas until they are complete. They can
  evolve from a `proposal` or can be submitted individually depending on the size.
- `bug`: These track bugs with the code
- `docs`: These track problems with the documentation (i.e. missing or incomplete)

### Issue Lifecycle

The issue lifecycle is mainly driven by the core maintainers, but is good information for those
contributing to CNCF Cloud Native App. All issue types follow the same general lifecycle. Differences are noted
below.

1. Issue creation
2. Triage
   - The maintainer in charge of triaging will apply the proper labels for the issue. This includes
     labels for priority, type, and metadata (such as `good first issue`). The only issue priority
     we will be tracking is whether or not the issue is "critical." If additional levels are needed
     in the future, we will add them.
   - (If needed) Clean up the title to succinctly and clearly state the issue. Also ensure that
     proposals are prefaced with "Proposal: [the rest of the title]".
   - Add the issue to the correct milestone. If any questions come up, don't worry about adding the
     issue to a milestone until the questions are answered.
   - We attempt to do this process at least once per work day.
3. Discussion
   - Issues that are labeled `feature` or `proposal` must write a CNCF Cloud Native App Improvement Proposal (HIP).
     See [Proposing an Idea](#proposing-an-idea). Smaller quality-of-life enhancements are exempt.
   - Issues that are labeled as `feature` or `bug` should be connected to the PR that resolves it.
   - Whoever is working on a `feature` or `bug` issue (whether a maintainer or someone from the
     community), should either assign the issue to themself or make a comment in the issue saying
     that they are taking it.
   - `proposal` and `support/question` issues should stay open until resolved or if they have not
     been active for more than 30 days. This will help keep the issue queue to a manageable size
     and reduce noise. Should the issue need to stay open, the `keep open` label can be added.
4. Issue closure

## Pull Requests

Like any good open source project, we use Pull Requests (PRs) to track code changes.

### PR Lifecycle

1. PR creation
   - PRs are usually created to fix or else be a subset of other PRs that fix a particular issue.
   - We more than welcome PRs that are currently in progress. They are a great way to keep track of
     important work that is in-flight, but useful for others to see. If a PR is a work in progress,
     it **must** be prefaced with "WIP: [title]". Once the PR is ready for review, remove "WIP"
     from the title.
   - It is preferred, but not required, to have a PR tied to a specific issue. There can be
     circumstances where if it is a quick fix then an issue might be overkill. The details provided
     in the PR description would suffice in this case.
2. Triage
   - The maintainer in charge of triaging will apply the proper labels for the issue. This should
     include at least a size label, `bug` or `feature`, and `awaiting review` once all labels are
     applied. See the [Labels section](#labels) for full details on the definitions of labels.
   - Add the PR to the correct milestone. This should be the same as the issue the PR closes.
3. Assigning reviews
   - Once a review has the `awaiting review` label, maintainers will review them as schedule
     permits. The maintainer who takes the issue should self-request a review.
   - PRs from a community member with the label `size/S` or larger requires 2 review approvals from
     maintainers before it can be merged. Those with `size/XS` are per the judgement of the
     maintainers. For more detail see the [Size Labels](#size-labels) section.
4. Reviewing/Discussion
   - All reviews will be completed using GitHub review tool.
   - A "Comment" review should be used when there are questions about the code that should be
     answered, but that don't involve code changes. This type of review does not count as approval.
   - A "Changes Requested" review indicates that changes to the code need to be made before they
     will be merged.
   - Reviewers should update labels as needed (such as `needs rebase`)
5. Address comments by answering questions or changing code
6. LGTM (Looks good to me)
   - Once a Reviewer has completed a review and the code looks ready to merge, an "Approve" review
     is used to signal to the contributor and to other maintainers that you have reviewed the code
     and feel that it is ready to be merged.
7. Merge or close
   - PRs should stay open until merged or if they have not been active for more than 30 days. This
     will help keep the PR queue to a manageable size and reduce noise. Should the PR need to stay
     open (like in the case of a WIP), the `keep open` label can be added.
   - Before merging a PR, refer to the topic on [Size Labels](#size-labels) below to determine if
     the PR requires more than one LGTM to merge.
   - If the owner of the PR is listed in the `OWNERS` file, that user **must** merge their own PRs
     or explicitly request another OWNER do that for them.
   - If the owner of a PR is _not_ listed in `OWNERS`, any core maintainer may merge the PR.

#### Documentation PRs

Documentation PRs will follow the same lifecycle as other PRs. They will also be labeled with the
`docs` label. For documentation, special attention will be paid to spelling, grammar, and clarity
(whereas those things don't matter _as_ much for comments in code).

## The Triager

Each week, one of the core maintainers will serve as the designated "triager" starting after the
public stand-up meetings on Thursday. This person will be in charge triaging new PRs and issues
throughout the work week.

## Labels

The following tables define all label types used for CNCF Cloud Native App. It is split up by category.

### Common

| Label       | Description                                                                                                                            |
| ----------- | -------------------------------------------------------------------------------------------------------------------------------------- |
| `bug`       | Marks an issue as a bug or a PR as a bugfix                                                                                            |
| `critical`  | Marks an issue or PR as critical. This means that addressing the PR or issue is top priority and must be addressed as soon as possible |
| `docs`      | Indicates the issue or PR is a documentation change                                                                                    |
| `feature`   | Marks the issue as a feature request or a PR as a feature implementation                                                               |
| `keep open` | Denotes that the issue or PR should be kept open past 30 days of inactivity                                                            |
| `refactor`  | Indicates that the issue is a code refactor and is not fixing a bug or adding additional functionality                                 |

### Issue Specific

| Label              | Description                                                                                     |
| ------------------ | ----------------------------------------------------------------------------------------------- |
| `help wanted`      | Marks an issue needs help from the community to solve                                           |
| `proposal`         | Marks an issue as a proposal                                                                    |
| `question/support` | Marks an issue as a support request or question                                                 |
| `good first issue` | Marks an issue as a good starter issue for someone new to CNCF Cloud Native App                 |
| `wont fix`         | Marks an issue as discussed and will not be implemented (or accepted in the case of a proposal) |

### PR Specific

| Label             | Description                                                                                                                                                             |
| ----------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `awaiting review` | Indicates a PR has been triaged and is ready for someone to review                                                                                                      |
| `breaking`        | Indicates a PR has breaking changes (such as API changes)                                                                                                               |
| `in progress`     | Indicates that a maintainer is looking at the PR, even if no review has been posted yet                                                                                 |
| `needs rebase`    | Indicates a PR needs to be rebased before it can be merged                                                                                                              |
| `needs pick`      | Indicates a PR needs to be cherry-picked into a feature branch (generally bugfix branches). Once it has been, the `picked` label should be applied and this one removed |
| `picked`          | This PR has been cherry-picked into a feature branch                                                                                                                    |

#### Size labels

Size labels are used to indicate how "dangerous" a PR is. The guidelines below are used to assign
the labels, but ultimately this can be changed by the maintainers. For example, even if a PR only
makes 30 lines of changes in 1 file, but it changes key functionality, it will likely be labeled as
`size/L` because it requires sign off from multiple people. Conversely, a PR that adds a small
feature, but requires another 150 lines of tests to cover all cases, could be labeled as `size/S`
even though the number of lines is greater than defined below.

Any changes from the community labeled as `size/S` or larger should be thoroughly tested before
merging and always requires approval from 2 core maintainers. PRs submitted by a core maintainer,
regardless of size, only requires approval from one additional maintainer. This ensures there are at
least two maintainers who are aware of any significant PRs introduced to the codebase.

| Label      | Description                                                                                                                 |
| ---------- | --------------------------------------------------------------------------------------------------------------------------- |
| `size/XS`  | Denotes a PR that changes 0-9 lines, ignoring generated files. Very little testing may be required depending on the change. |
| `size/S`   | Denotes a PR that changes 10-29 lines, ignoring generated files. Only small amounts of manual testing may be required.      |
| `size/M`   | Denotes a PR that changes 30-99 lines, ignoring generated files. Manual validation should be required.                      |
| `size/L`   | Denotes a PR that changes 100-499 lines, ignoring generated files.                                                          |
| `size/XL`  | Denotes a PR that changes 500-999 lines, ignoring generated files.                                                          |
| `size/XXL` | Denotes a PR that changes 1000+ lines, ignoring generated files.                                                            |
