import { MetadataUser } from "@/lib/metadata";
import { Avatar, AvatarFallback } from "./ui";

function UserLabel({ user }: { user: MetadataUser }) {
  return (
    <span className="inline-flex gap-2 items-center">
      <Avatar>
        <AvatarFallback />
      </Avatar>
      {`${user.name ?? ""} (${user.id})`}
    </span>
  );
}

export { UserLabel };
