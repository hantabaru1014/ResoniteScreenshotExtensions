/* eslint @typescript-eslint/no-explicit-any: 0 */

import { XMLParser } from "fast-xml-parser";
import {
  float3,
  Metadata,
  MetadataUser,
  MetadataUserInfo,
  SessionAccessLevel,
  StereoLayout,
} from "./metadata";

const unescapeUnicode = (str: string) => {
  return str
    .replace(/(?<!\\)\\u([0-9A-Fa-f]{4})/g, (_, p1) => {
      return String.fromCharCode(parseInt(p1, 16));
    })
    .replace(/\\\\u/g, "\\u");
};

export const loadXMP = async (file: File) => {
  const fileContent = await file.text();
  const match = fileContent.match(
    /<rdf:RDF xmlns:rdf="http:\/\/www\.w3\.org\/1999\/02\/22-rdf-syntax-ns#">(.+)<\/rdf:RDF>/,
  );
  if (!match) {
    return null;
  }
  return unescapeUnicode(match[0]);
};

const parseComponentJson = (json: string) => {
  const obj = JSON.parse(json);
  const component = obj["Component"];
  if (!component) {
    throw new Error("Invalid JSON format");
  }
  const metadata: Metadata = {
    locationName: component["LocationName"]["Data"],
    locationURL: component["LocationURL"]["Data"],
    locationHost: {
      id: component["LocationHost"]["_userId"]["Data"],
      machineId: component["LocationHost"]["_machineId"]["Data"],
    },
    locationAccessLevel: component["LocationAccessLevel"]["Data"],
    locationHiddenFromListing: component["LocationHiddenFromListing"]["Data"],
    timeTaken: new Date(component["TimeTaken"]["Data"]),
    takenBy: {
      id: component["TakenBy"]["_userId"]["Data"],
      machineId: component["TakenBy"]["_machineId"]["Data"],
    },
    takenGlobalPosition: component["TakenGlobalPosition"]["Data"],
    takenGlobalRotation: component["TakenGlobalRotation"]["Data"],
    takenGlobalScale: component["TakenGlobalScale"]["Data"],
    appVersion: component["AppVersion"]["Data"],
    userInfos: component["UserInfos"]["Data"].map((userInfo: any) => ({
      user: {
        id: userInfo["User"]["_userId"]["Data"],
        machineId: userInfo["User"]["_machineId"]["Data"],
      },
      isInVR: userInfo["IsInVR"]["Data"],
      isPresent: userInfo["IsPresent"]["Data"],
      headPosition: userInfo["HeadPosition"]["Data"],
      headOrientation: userInfo["HeadOrientation"]["Data"],
      sessionJoinTimestamp: new Date(userInfo["SessionJoinTimestamp"]["Data"]),
    })),
    cameraManufacturer: component["CameraManufacturer"]["Data"],
    cameraModel: component["CameraModel"]["Data"],
    cameraFOV: component["CameraFOV"]["Data"],
    is360: component["Is360"]["Data"],
    stereoLayout: component["StereoLayout"]["Data"],
  };

  return metadata;
};

const parseXML = (xmlObj: any) => {
  const parseUser = (obj: any) => {
    const prefix = "rse:U-";
    return Object.fromEntries(
      Object.entries(obj)
        .filter((e) => e[0].startsWith(prefix))
        .map((e) => {
          const key = e[0].substring(prefix.length);
          const newKey = key.charAt(0).toLowerCase() + key.slice(1);
          return [newKey, e[1]];
        }),
    ) as MetadataUser;
  };
  const parseFloat3 = (str: string) => {
    const matched = str.match(/^\[(.+); (.+); (.+)\]$/);
    if (!matched) {
      throw new Error("Invalid format");
    }
    return [matched[1], matched[2], matched[3]].map(Number) as float3;
  };
  const parseFloatQ = (str: string) => {
    const matched = str.match(/^\[(.+); (.+); (.+); (.+)\]$/);
    if (!matched) {
      throw new Error("Invalid format");
    }
    return [matched[1], matched[2], matched[3], matched[4]].map(
      Number,
    ) as float3;
  };
  const parseUserInfo = (obj: any) => {
    const user = parseUser(obj);
    const prefix = "rse:UI-";
    const info = Object.fromEntries(
      Object.entries(obj)
        .filter((e) => e[0].startsWith(prefix))
        .map((e) => {
          const key = e[0].substring(prefix.length);
          const newKey = key.charAt(0).toLowerCase() + key.slice(1);

          let newValue = e[1];
          if (newKey === "headPosition") {
            newValue = parseFloat3(e[1] as string);
          }
          if (newKey === "headOrientation") {
            newValue = parseFloatQ(e[1] as string);
          }
          if (newKey === "sessionJoinTimestamp") {
            newValue = new Date(e[1] as string);
          }

          return [newKey, newValue];
        }),
    );
    info["user"] = user;

    return info as MetadataUserInfo;
  };
  const parseMetadata = (obj: any) => {
    const prefix = "rse:";
    return Object.fromEntries(
      Object.entries(obj)
        .filter((e) => e[0].startsWith(prefix))
        .map((e) => {
          const key = e[0].substring(prefix.length);
          const newKey = key.charAt(0).toLowerCase() + key.slice(1);

          let newValue = e[1];
          if (newKey === "takenGlobalPosition") {
            newValue = parseFloat3(e[1] as string);
          }
          if (newKey === "takenGlobalRotation") {
            newValue = parseFloatQ(e[1] as string);
          }
          if (newKey === "takenGlobalScale") {
            newValue = parseFloat3(e[1] as string);
          }
          if (newKey === "timeTaken") {
            newValue = new Date(e[1] as string);
          }
          if (newKey === "locationHost") {
            newValue = parseUser(e[1]);
          }
          if (newKey === "takenBy") {
            newValue = parseUser(e[1]);
          }
          if (newKey === "userInfos" && Array.isArray(e[1])) {
            newValue = e[1].map((o: any) => parseUserInfo(o["rse:UserInfo"]));
          }
          if (newKey === "locationAccessLevel") {
            newValue = e[1] as keyof typeof SessionAccessLevel;
          }
          if (newKey === "stereoLayout") {
            newValue = e[1] as keyof typeof StereoLayout;
          }

          return [newKey, newValue];
        }),
    ) as Metadata;
  };

  return parseMetadata(xmlObj);
};

export const parseMetadata = (xml: string) => {
  const parser = new XMLParser({
    ignoreAttributes: false,
    attributeNamePrefix: "",
    isArray: (tagName) => ["rdf:RDF", "rse:UserInfos"].includes(tagName),
    parseAttributeValue: true,
  });
  const xmpData = parser.parse(xml);
  if (!xmpData?.["rdf:RDF"]) {
    throw new Error("XMP data not found");
  }
  const modDescription = Array.isArray(xmpData["rdf:RDF"])
    ? xmpData["rdf:RDF"]
        .filter(
          (item) =>
            item["rdf:Description"]?.["xmlns:rse"] != null ||
            item["rdf:Description"]?.["xmlns:resonite-ss-ext"] != null,
        )
        .map((item) => item["rdf:Description"])
    : [];
  if (modDescription.length !== 1) {
    throw new Error("Invalid XMP format");
  }
  const obj = modDescription[0];
  if (obj["xmlns:rse"] != null) {
    return parseXML(obj);
  }
  if (obj["resonite-ss-ext:PhotoMetadataJson"] != null) {
    return parseComponentJson(obj["resonite-ss-ext:PhotoMetadataJson"]);
  }

  throw new Error("Invalid XMP format");
};

export const loadXMPMetadata = async (file: File) => {
  const xmp = await loadXMP(file);
  if (!xmp) {
    return null;
  }
  return parseMetadata(xmp);
};
