namespace Esports.Api;

// The shape of data arriving from, or going to, the outside world.
//
// WHY THESE EXIST AND ARE NOT JUST `Team`:
//
// A caller creating a team sends a name and a tag. It must NOT send an Id -
// the database generates that. If the endpoint accepted a `Team` directly,
// a caller could post {"id": 999, ...} and set it themselves.
//
// That attack has a name - over-posting - and it is the reason the thing you
// accept is almost never the thing you store. The incoming shape is a
// CONTRACT with the caller; your domain class is your own business, and the
// two change for different reasons.
//
// A `record` because this IS its contents (Day 3) - two requests with the
// same name and tag are the same request.
public record CreateTeamRequest(string Name, string Tag);

// The shape going back out. Same reasoning in reverse: the caller gets
// exactly the fields chosen here, not whatever Team happens to hold today.
public record TeamResponse(int Id, string Name, string Tag);

public record CreatePlayerRequest(string GamerTag, int Rating);

public record PlayerResponse(int Id, string GamerTag, int Rating, int TeamId);
