/* eslint @typescript-eslint/no-explicit-any: 0 */

import { XMLParser } from "fast-xml-parser";
import {
  float3,
  floatQ,
  Metadata,
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
  const parseFloat3 = (str: string | undefined) => {
    if (str === undefined) {
      return [0, 0, 0] as float3;
    }
    const matched = str.match(/^\[(.+); (.+); (.+)\]$/);
    if (!matched) {
      throw new Error("Invalid format");
    }
    return [matched[1], matched[2], matched[3]].map(Number) as float3;
  };
  const parseFloatQ = (str: string | undefined) => {
    if (str === undefined) {
      return [0, 0, 0, 1] as floatQ;
    }
    const matched = str.match(/^\[(.+); (.+); (.+); (.+)\]$/);
    if (!matched) {
      throw new Error("Invalid format");
    }
    return [matched[1], matched[2], matched[3], matched[4]].map(
      Number,
    ) as floatQ;
  };

  const metadata: Metadata = {
    locationName: xmlObj["rse:LocationName"],
    locationURL: xmlObj["rse:LocationURL"],
    locationHost: {
      id: xmlObj["rse:LocationHost"]["rse:U-Id"],
      name: xmlObj["rse:LocationHost"]["rse:U-Name"],
      machineId: xmlObj["rse:LocationHost"]["rse:U-MachineId"],
    },
    locationAccessLevel: xmlObj["rse:LocationAccessLevel"],
    locationHiddenFromListing: xmlObj["rse:LocationHiddenFromListing"],
    timeTaken: new Date(xmlObj["rse:TimeTaken"]),
    takenBy: {
      id: xmlObj["rse:TakenBy"]["rse:U-Id"],
      name: xmlObj["rse:TakenBy"]["rse:U-Name"],
      machineId: xmlObj["rse:TakenBy"]["rse:U-MachineId"],
    },
    takenGlobalPosition: parseFloat3(xmlObj["rse:TakenGlobalPosition"]),
    takenGlobalRotation: parseFloatQ(xmlObj["rse:TakenGlobalRotation"]),
    takenGlobalScale: parseFloat3(xmlObj["rse:TakenGlobalScale"]),
    appVersion: xmlObj["rse:AppVersion"],
    userInfos: xmlObj["rse:UserInfos"].map((o: any) => {
      const info = o["rse:UserInfo"];

      return {
        user: {
          id: info["rse:U-Id"],
          name: info["rse:U-Name"],
          machineId: info["rse:U-MachineId"],
        },
        isInVR: info["rse:UI-IsInVR"],
        isPresent: info["rse:UI-IsPresent"],
        headPosition: parseFloat3(info["rse:UI-HeadPosition"]),
        headOrientation: parseFloatQ(info["rse:UI-HeadOrientation"]),
        sessionJoinTimestamp: new Date(info["rse:UI-SessionJoinTimestamp"]),
      };
    }),
    cameraManufacturer: xmlObj["rse:CameraManufacturer"],
    cameraModel: xmlObj["rse:CameraModel"],
    cameraFOV: xmlObj["rse:CameraFOV"],
    is360: xmlObj["rse:Is360"],
    stereoLayout: xmlObj["rse:StereoLayout"],
  };

  return metadata;
};

export const parseMetadata = (xml: string) => {
  const parser = new XMLParser({
    ignoreAttributes: false,
    attributeNamePrefix: "",
    isArray: (tagName) =>
      ["rdf:Description", "rse:UserInfos"].includes(tagName),
    parseAttributeValue: true,
  });
  const xmpData = parser.parse(xml);
  if (!xmpData?.["rdf:RDF"]) {
    throw new Error("XMP data not found");
  }
  const modDescription = Array.isArray(xmpData["rdf:RDF"]?.["rdf:Description"])
    ? xmpData["rdf:RDF"]?.["rdf:Description"].filter(
        (item) =>
          item["xmlns:rse"] != null || item["xmlns:resonite-ss-ext"] != null,
      )
    : [];
  if (modDescription.length === 0) {
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
