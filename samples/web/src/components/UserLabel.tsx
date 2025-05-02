import { MetadataUser } from "@/lib/metadata";
import { Avatar, AvatarFallback, AvatarImage } from "./ui";
import { useQuery } from "@tanstack/react-query";
import { resolveResdbUrl } from "@/lib/utils";

type CloudUserInfo = {
  id: string;
  username: string;
  iconUrl?: string;
};

function UserLabel({ user }: { user: MetadataUser }) {
  const { data, isSuccess } = useQuery({
    queryKey: ["users", user.id],
    queryFn: () => fetch(`/api/users/${user.id}`).then((res) => res.json()),
    enabled: !!user.id,
    staleTime: Infinity,
    select: (data): CloudUserInfo => ({
      id: data.id,
      username: data.username,
      iconUrl: resolveResdbUrl(data.profile?.iconUrl),
    }),
  });

  return (
    <span className="inline-flex gap-2 items-center">
      <Avatar>
        {isSuccess && data.iconUrl && (
          <AvatarImage src={data.iconUrl} alt="User Icon" />
        )}
        <AvatarFallback />
      </Avatar>
      {`${data?.username ?? user.name ?? ""} (${user.id})`}
    </span>
  );
}

export { UserLabel };
