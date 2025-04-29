import { Metadata } from "@/lib/metadata";
import {
  Table,
  TableBody,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui";

function MetadataDisplayPanel({ value }: { value?: Metadata }) {
  return (
    <div>
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Key</TableHead>
            <TableHead>Value</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {value &&
            Object.entries(value).map((item) => {
              const [key, val] = item;
              return (
                <TableRow key={key}>
                  <td>{key}</td>
                  <td>
                    {typeof val === "object"
                      ? JSON.stringify(val)
                      : String(val)}
                  </td>
                </TableRow>
              );
            })}
        </TableBody>
      </Table>
    </div>
  );
}

export { MetadataDisplayPanel };
