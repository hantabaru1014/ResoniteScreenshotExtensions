import { Metadata, MetadataUser } from "@/lib/metadata";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui";
import { UserLabel } from "./UserLabel";

function MetadataTable({
  value,
  hiddenKeys,
}: {
  value?: Metadata;
  hiddenKeys?: string[];
}) {
  const renderValue = (key: keyof Metadata, value: Metadata[typeof key]) => {
    switch (key) {
      case "locationHost":
      case "takenBy":
        return <UserLabel user={value as MetadataUser} />;
      case "timeTaken":
        return <span>{(value as Date).toLocaleString()}</span>;
      default:
        return (
          <span>
            {typeof value === "object" ? JSON.stringify(value) : String(value)}
          </span>
        );
    }
  };

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Key</TableHead>
          <TableHead>Value</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {value &&
          Object.entries(value)
            .filter((e) => !hiddenKeys || !hiddenKeys.includes(e[0]))
            .map((item) => {
              const [key, val] = item;
              return (
                <TableRow key={key}>
                  <TableCell>{key}</TableCell>
                  <TableCell>
                    {renderValue(key as keyof Metadata, val)}
                  </TableCell>
                </TableRow>
              );
            })}
      </TableBody>
    </Table>
  );
}

function UsersTable({ value }: { value?: Metadata }) {
  return (
    <>
      <span>Users</span>
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>User</TableHead>
            <TableHead>VR</TableHead>
            <TableHead>AFK</TableHead>
            <TableHead>Head Pos</TableHead>
            <TableHead>Head Rot</TableHead>
            <TableHead>Session Join</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {value?.userInfos.map((userInfo) => {
            return (
              <TableRow key={userInfo.user.id}>
                <TableCell>
                  <UserLabel user={userInfo.user} />
                </TableCell>
                <TableCell>{userInfo.isInVR ? "Yes" : "No"}</TableCell>
                <TableCell>{userInfo.isPresent ? "No" : "Yes"}</TableCell>
                <TableCell>{JSON.stringify(userInfo.headPosition)}</TableCell>
                <TableCell>
                  {JSON.stringify(userInfo.headOrientation)}
                </TableCell>
                <TableCell>
                  {userInfo.sessionJoinTimestamp.toLocaleString()}
                </TableCell>
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
    </>
  );
}

function MetadataDisplayPanel({ value }: { value?: Metadata }) {
  return (
    <div>
      <MetadataTable value={value} hiddenKeys={["userInfos"]} />
      {value && <UsersTable value={value} />}
    </div>
  );
}

export { MetadataDisplayPanel };
